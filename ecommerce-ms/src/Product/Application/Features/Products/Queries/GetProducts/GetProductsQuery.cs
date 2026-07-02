using MediatR;
using Product.Application.DTOs;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(int Page = 1, int PageSize = 20, string? Category = null) : IRequest<ApiResponse<PagedResult<ProductDto>>>, ICacheableQuery
{
    public string CacheKey => $"products:{Page}:{PageSize}:{Category ?? "all"}";
}
