using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid Id, string Name, string Phone,
    string Street, string City, string State, string ZipCode, string Country)
    : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "Update";
    public string AuditResource => "Customer";
    public string? AuditResourceId => Id.ToString();
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["customers:all", $"customers:{Id}"];
}
