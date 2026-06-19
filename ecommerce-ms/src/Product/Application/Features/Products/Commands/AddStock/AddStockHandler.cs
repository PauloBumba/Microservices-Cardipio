using MediatR;
using Product.Application.Repositories;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.AddStock;

public sealed class AddStockHandler(IProductRepository repo)
    : IRequestHandler<AddStockCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(AddStockCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return ApiResponse<bool>.Fail("Produto não encontrado.");
        product.AddStock(cmd.Quantity);
        return ApiResponse<bool>.Ok(true);
    }
}
