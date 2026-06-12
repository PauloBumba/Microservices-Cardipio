using MediatR;
using Product.Application.DTOs;
namespace Product.Application.Features.Products.Queries.GetProductById;
public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
