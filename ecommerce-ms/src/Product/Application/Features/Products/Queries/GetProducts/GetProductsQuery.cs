using MediatR;
using Product.Application.DTOs;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery : IRequest<ApiResponse<List<ProductDto>>>, ICacheableQuery
{
    public string CacheKey => "products:all";
}
