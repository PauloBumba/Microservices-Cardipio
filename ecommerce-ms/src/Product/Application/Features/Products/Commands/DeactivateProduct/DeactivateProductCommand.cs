using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Product.Application.Features.Products.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid Id)
    : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "Deactivate";
    public string AuditResource => "Product";
    public string? AuditResourceId => Id.ToString();
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["products:all", $"products:{Id}"];
}
