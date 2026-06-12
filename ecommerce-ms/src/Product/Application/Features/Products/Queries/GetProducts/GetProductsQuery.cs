using MediatR;
using Product.Application.DTOs;
namespace Product.Application.Features.Products.Queries.GetProducts;
public sealed record GetProductsQuery(int Page=1, int PageSize=20, string? Category=null) : IRequest<PagedResult<ProductDto>>;
