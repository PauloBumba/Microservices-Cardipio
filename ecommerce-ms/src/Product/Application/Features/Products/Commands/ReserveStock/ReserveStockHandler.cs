using MediatR;
using Microsoft.Extensions.Logging;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;
using Shared.Application.Response;
namespace Product.Application.Features.Products.Commands.ReserveStock;
public sealed class ReserveStockHandler(IProductRepository repo, IUnitOfWork uow, ILogger<ReserveStockHandler> log)
    : IRequestHandler<ReserveStockCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(ReserveStockCommand cmd, CancellationToken ct)
    {
        var p = await repo.GetByIdTrackedAsync(cmd.ProductId, ct);
        if (p is null) return ApiResponse<bool>.Fail("Produto não encontrado");
        try 
        { 
            p.ReserveStock(cmd.Quantity); 
            await uow.CommitAsync(ct); 
            return ApiResponse<bool>.Ok(true); 
        }
        catch (InsufficientStockException ex) 
        { 
            log.LogWarning(ex, "Estoque insuficiente: {Id}", cmd.ProductId); 
            return ApiResponse<bool>.Fail("Estoque insuficiente"); 
        }
    }
}
