using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.API.DTOs.Order.API.DTOs;
using Order.API.Model;
using Shared;

namespace Order.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        public OrdersController(AppDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> Create(OrderCreateDto orderCreate)
        {
            try
            {
                var newOrder = new Model.Order
                {
                    BuyerId = orderCreate.BuyerId,
                    Status = OrderStatus.Suspend,
                    Address = new Address { Line = orderCreate.Address.Line, Province = orderCreate.Address.Province, District = orderCreate.Address.District },
                    CreatedDate = DateTime.Now,
                    FailMessage = "test"
                };

                orderCreate.orderItems.ForEach(item =>
                {
                    newOrder.Items.Add(new OrderItem() { Price = item.Price, ProductId = item.ProductId, Count = item.Count });
                });

                await _context.AddAsync(newOrder);

                await _context.SaveChangesAsync();


                var orderCreatedEvent = new OrderCreatedEvent()
                {
                    BuyerId = orderCreate.BuyerId,
                    OrderId = newOrder.Id,
                    Payment = new PaymentMessage
                    {
                        CardName = orderCreate.payment.CardName,
                        CardNumber = orderCreate.payment.CardNumber,
                        Expiration = orderCreate.payment.Expiration,
                        CVV = orderCreate.payment.CVV,
                        TotalPrice = orderCreate.orderItems.Sum(x => x.Price * x.Count)
                    },
                };

                orderCreate.orderItems.ForEach(item =>
                {
                    orderCreatedEvent.orderItems.Add(new OrderItemMessage { Count = item.Count, ProductId = item.ProductId });
                });

                await _publishEndpoint.Publish(orderCreatedEvent);

                return Ok();
            }
            catch (Exception e)
            {
                return Ok();

            }

        }
    }
}
