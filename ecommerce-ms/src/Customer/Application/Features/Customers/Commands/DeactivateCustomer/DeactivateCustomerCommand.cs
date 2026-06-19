using MediatR;
using Shared.Application.Behaviors;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Commands.DeactivateCustomer;

public sealed record DeactivateCustomerCommand(Guid Id)
    : IRequest<ApiResponse<bool>>, IBaseCommand, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["customers:all", $"customers:{Id}"];
}
