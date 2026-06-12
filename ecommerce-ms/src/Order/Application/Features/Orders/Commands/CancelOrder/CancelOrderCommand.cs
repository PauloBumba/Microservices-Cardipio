using MediatR;
namespace Order.Application.Features.Orders.Commands.CancelOrder;
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest;
