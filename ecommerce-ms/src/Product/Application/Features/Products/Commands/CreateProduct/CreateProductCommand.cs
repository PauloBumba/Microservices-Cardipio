using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name, string Description, string Sku,
    decimal Price, string Currency, int InitialStock, string Category)
    : IRequest<ApiResponse<Guid>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "Create";
    public string AuditResource => "Product";
    public string? AuditResourceId => null;
    public IEnumerable<string> CacheKeysToInvalidate => ["products:all"];
}
