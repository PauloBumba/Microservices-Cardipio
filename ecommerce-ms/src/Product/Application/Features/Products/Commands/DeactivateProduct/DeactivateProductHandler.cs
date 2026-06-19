using MediatR;
using Product.Application.Repositories;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.DeactivateProduct;

public sealed class DeactivateProductHandler(IProductRepository repo)
    : IRequestHandler<DeactivateProductCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeactivateProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.Id, ct);
        if (product is null) return ApiResponse<bool>.Fail("Produto não encontrado.");
        product.Deactivate();
        return ApiResponse<bool>.Ok(true);
    }
}
