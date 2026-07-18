using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Commands.DeactivateCustomer;

public sealed record DeactivateCustomerCommand(Guid Id)
    : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "Deactivate";
    public string AuditResource => "Customer";
    public string? AuditResourceId => Id.ToString();
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["customers:all", $"customers:{Id}"];
}
