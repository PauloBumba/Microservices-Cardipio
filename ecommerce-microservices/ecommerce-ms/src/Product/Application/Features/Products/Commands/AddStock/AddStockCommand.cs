using MediatR;
using Product.Application.DTOs;
namespace Product.Application.Features.Products.Commands.AddStock;
public sealed record AddStockCommand(Guid ProductId, int Quantity) : IRequest<StockInfoDto>;
