using MediatR;
using Product.Application.DTOs;
using Product.Application.Repositories;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdHandler(IProductRepository repo)
    : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>
{
    public async Task<ApiResponse<ProductDto>> Handle(GetProductByIdQuery query, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(query.Id, ct);
        if (p is null) return ApiResponse<ProductDto>.Fail("Produto não encontrado.");
        return ApiResponse<ProductDto>.Ok(new ProductDto(
            p.Id, p.Name, p.Description, p.Sku,
            p.Price.Amount, p.Price.Currency,
            p.StockQuantity, p.ReservedQuantity, p.AvailableQuantity,
            p.Category, p.IsActive, p.CreatedAt, p.UpdatedAt));
    }
}
