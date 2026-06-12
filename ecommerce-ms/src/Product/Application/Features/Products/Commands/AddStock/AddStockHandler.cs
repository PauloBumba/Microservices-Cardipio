using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.DTOs;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;
namespace Product.Application.Features.Products.Commands.AddStock;
public sealed class AddStockHandler(IProductRepository repo, IUnitOfWork uow, ILogger<AddStockHandler> log)
    : IRequestHandler<AddStockCommand, StockInfoDto>
{
    public async Task<StockInfoDto> Handle(AddStockCommand cmd, CancellationToken ct)
    {
        var p = await repo.GetByIdTrackedAsync(cmd.ProductId, ct) ?? throw new ProductNotFoundException(cmd.ProductId);
        p.AddStock(cmd.Quantity);
        await uow.CommitAsync(ct);
        log.LogInformation("Estoque adicionado: {Id} +{Qty}", p.Id, cmd.Quantity);
        return new StockInfoDto(p.Id, p.Sku, p.StockQuantity, p.ReservedQuantity, p.AvailableQuantity);
    }
}
