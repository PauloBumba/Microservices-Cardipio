using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.DTOs;
using Product.Application.Mappings;
using Product.Domain.Entities;
using Product.Domain.Repositories;
namespace Product.Application.Features.Products.Commands.CreateProduct;
public sealed class CreateProductHandler(
    IProductRepository repo, IUnitOfWork uow, ILogger<CreateProductHandler> log)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        if (await repo.SkuExistsAsync(cmd.Sku, ct))
            throw new InvalidOperationException($"SKU '{cmd.Sku}' já existe.");
        var p = Productss.Create(cmd.Name,cmd.Description,cmd.Sku,cmd.Price,cmd.Currency,cmd.InitialStock,cmd.Category);
        await repo.AddAsync(p, ct);
        await uow.CommitAsync(ct);
        log.LogInformation("Produto criado: {Id} {Sku}", p.Id, p.Sku);
        return p.ToDto();
    }
}
