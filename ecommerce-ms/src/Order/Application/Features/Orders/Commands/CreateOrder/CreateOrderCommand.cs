using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Order.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderItemDto(Guid ProductId, string ProductName, string Sku, int Quantity, decimal UnitPrice, string Currency);

public sealed record CreateOrderCommand(Guid CustomerId, List<CreateOrderItemDto> Items)
    : IRequest<ApiResponse<Guid>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "Create";
    public string AuditResource => "Order";
    public string? AuditResourceId => null;
    public IEnumerable<string> CacheKeysToInvalidate => 
        [$"orders:customer:{CustomerId}", "orders:all"];
}
