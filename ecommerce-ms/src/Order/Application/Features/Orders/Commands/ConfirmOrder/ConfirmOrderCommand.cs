using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Response;
namespace Order.Application.Features.Orders.Commands.ConfirmOrder;
public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest<ApiResponse<bool>>, IBaseCommand, IAuditableCommand
{
    public string AuditAction => "Confirm";
    public string AuditResource => "Order";
    public string? AuditResourceId => OrderId.ToString();
}
