using MediatR;
using Product.Application.DTOs;
using Product.Application.Mappings;
using Product.Domain.Repositories;
namespace Product.Application.Features.Products.Queries.GetProducts;
public sealed class GetProductsHandler(IProductRepository repo) : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery q, CancellationToken ct)
    {
        var items = await repo.GetPagedAsync(q.Page, q.PageSize, q.Category, ct);
        var total = await repo.CountAsync(q.Category, ct);
        return new PagedResult<ProductDto>(items.Select(p => p.ToDto()).ToList(), total, q.Page, q.PageSize);
    }
}
