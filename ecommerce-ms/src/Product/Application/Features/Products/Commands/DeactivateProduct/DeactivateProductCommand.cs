using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid Id)
    : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["products:all", $"products:{Id}"];
}
