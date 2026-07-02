using MediatR;
using Product.Application.DTOs;
using Product.Application.Repositories;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsHandler(IProductRepository repo)
    : IRequestHandler<GetProductsQuery, ApiResponse<PagedResult<ProductDto>>>
{
    public async Task<ApiResponse<PagedResult<ProductDto>>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        var list = await repo.GetAllAsync(ct);
        var dtos = list.Select(p => new ProductDto(
            p.Id, p.Name, p.Description, p.Sku,
            p.Price.Amount, p.Price.Currency,
            p.StockQuantity, p.ReservedQuantity, p.AvailableQuantity,
            p.Category, p.IsActive, p.CreatedAt, p.UpdatedAt)).ToList();
        
        return ApiResponse<PagedResult<ProductDto>>.Ok(new PagedResult<ProductDto>(dtos, dtos.Count, query.Page, query.PageSize));
    }
}
