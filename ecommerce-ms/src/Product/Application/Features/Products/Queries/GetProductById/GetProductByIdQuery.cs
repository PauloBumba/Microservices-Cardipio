using MediatR;
using Product.Application.DTOs;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ApiResponse<ProductDto>>, ICacheableQuery
{
    public string CacheKey => $"products:{Id}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}
