using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    string Name, string Email, string Phone,
    string Street, string City, string State, string ZipCode, string Country)
    : IRequest<ApiResponse<Guid>>, IBaseCommand, ICacheInvalidator, IAuditableCommand
{
    public string AuditAction => "Create";
    public string AuditResource => "Customer";
    public string? AuditResourceId => null;
    public IEnumerable<string> CacheKeysToInvalidate => ["customers:all"];
}
