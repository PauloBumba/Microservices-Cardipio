using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.ReserveStock;

public sealed record ReserveStockCommand(Guid ProductId, int Quantity) 
    : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "ReserveStock";
    public string AuditResource => "Product";
    public string? AuditResourceId => ProductId.ToString();
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["products:all", $"products:{ProductId}"];
}
