using MediatR;
namespace Product.Application.Features.Products.Commands.ReserveStock;
public sealed record ReserveStockCommand(Guid ProductId, int Quantity) : IRequest<bool>;
