using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Repositories;
using Product.Domain.Entities;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository repo,
    ILogger<CreateProductHandler> logger)
    : IRequestHandler<CreateProductCommand, ApiResponse<Guid>>
{
    public async Task<ApiResponse<Guid>> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        if (await repo.SkuExistsAsync(cmd.Sku, ct))
            return ApiResponse<Guid>.Fail($"SKU '{cmd.Sku}' já cadastrado.");

        var product = Productss.Create(
            cmd.Name, cmd.Description, cmd.Sku,
            cmd.Price, cmd.Currency, cmd.InitialStock, cmd.Category);

        await repo.AddAsync(product, ct);
        logger.LogInformation("Produto criado: {Id} SKU={Sku}", product.Id, product.Sku);
        return ApiResponse<Guid>.Ok(product.Id);
    }
}
