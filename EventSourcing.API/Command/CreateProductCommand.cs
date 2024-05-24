using EventSourcing.API.DTOs;
using MediatR;

namespace EventSourcing.API.Command
{
    public class CreateProductCommand : IRequest
    {
        public CreateProductDto CreateProductDto { get; set; }
    }
}