using MediatR;
using Microsoft.Extensions.Logging;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;
namespace Product.Application.Features.Products.Commands.ReserveStock;
public sealed class ReserveStockHandler(IProductRepository repo, IUnitOfWork uow, ILogger<ReserveStockHandler> log)
    : IRequestHandler<ReserveStockCommand, bool>
{
    public async Task<bool> Handle(ReserveStockCommand cmd, CancellationToken ct)
    {
        var p = await repo.GetByIdTrackedAsync(cmd.ProductId, ct);
        if (p is null) return false;
        try { p.ReserveStock(cmd.Quantity); await uow.CommitAsync(ct); return true; }
        catch (InsufficientStockException ex) { log.LogWarning(ex, "Estoque insuficiente: {Id}", cmd.ProductId); return false; }
    }
}
