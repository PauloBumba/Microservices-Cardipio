using MediatR;
using Product.Application.DTOs;
using Product.Application.Repositories;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsHandler(IProductRepository repo)
    : IRequestHandler<GetProductsQuery, ApiResponse<List<ProductDto>>>
{
    public async Task<ApiResponse<List<ProductDto>>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        var list = await repo.GetAllAsync(ct);
        return ApiResponse<List<ProductDto>>.Ok(list.Select(p => new ProductDto(
            p.Id, p.Name, p.Description, p.Sku,
            p.Price.Amount, p.Price.Currency,
            p.StockQuantity, p.ReservedQuantity, p.AvailableQuantity,
            p.Category, p.IsActive, p.CreatedAt, p.UpdatedAt)).ToList());
    }
}
