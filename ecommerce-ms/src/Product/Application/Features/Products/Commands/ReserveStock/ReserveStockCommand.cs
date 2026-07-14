using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.ReserveStock;

public sealed record ReserveStockCommand(Guid ProductId, int Quantity) 
    : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["products:all", $"products:{ProductId}"];
}
