using MediatR;
using Product.Application.DTOs;
namespace Product.Application.Features.Products.Commands.CreateProduct;
public sealed record CreateProductCommand(
    string Name, string Description, string Sku,
    decimal Price, string Currency, int InitialStock, string Category) : IRequest<ProductDto>;
