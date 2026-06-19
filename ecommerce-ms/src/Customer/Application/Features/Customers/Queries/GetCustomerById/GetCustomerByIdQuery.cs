using Customer.Application.DTOs;
using MediatR;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid Id)
    : IRequest<ApiResponse<CustomerDto>>, ICacheableQuery
{
    public string CacheKey => $"customers:{Id}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}
