using Customer.Application.DTOs;
using MediatR;
using Shared.Application.Caching;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Queries.GetCustomers;

public sealed record GetCustomersQuery : IRequest<ApiResponse<List<CustomerDto>>>, ICacheableQuery
{
    public string CacheKey => "customers:all";
}
