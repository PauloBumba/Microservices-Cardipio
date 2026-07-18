using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;
namespace Order.Application.Features.Orders.Commands.CancelOrder;
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "Cancel";
    public string AuditResource => "Order";
    public string? AuditResourceId => OrderId.ToString();
    public IEnumerable<string> CacheKeysToInvalidate => [$"orders:{OrderId}", "orders:all"];
}
