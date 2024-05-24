using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaStateMachineWorkerService;
using SagaStateMachineWorkerService.Model;
using Shared;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

// Worker hizmetini ekler
builder.Services.AddHostedService<Worker>();

// MassTransit ve EntityFramework yapılandırması
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddSagaStateMachine<OrderStateMachine, OrderStateInstance>().EntityFrameworkRepository(opt =>
    {
        opt.AddDbContext<DbContext, OrderStateDbContext>((provider, optionsBuilder) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("SqlCon"), m =>
            {
                m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            });
        });
    });

    cfg.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(configure =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        configure.Host(configuration.GetConnectionString("RabbitMQ"));

        configure.UseMessageRetry(retryConfig => retryConfig.Immediate(4));

        configure.ReceiveEndpoint(RabbitMQSettingsConst.OrderSaga, e =>
        {
            e.ConfigureSaga<OrderStateInstance>(provider);
        });
    }));
});

var host = builder.Build();
await host.RunAsync();
