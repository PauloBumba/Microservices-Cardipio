using Customer.Application.DTOs;
using Customer.Application.Mappings;
using Customer.Application.Repositories;
using MediatR;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Queries.GetCustomers;

public sealed class GetCustomersHandler(ICustomerRepository repo)
    : IRequestHandler<GetCustomersQuery, ApiResponse<List<CustomerDto>>>
{
    public async Task<ApiResponse<List<CustomerDto>>> Handle(GetCustomersQuery query, CancellationToken ct)
    {
        var list = await repo.GetPagedAsync(1, 100, ct);
        return ApiResponse<List<CustomerDto>>.Ok(list.Select(c => c.ToDto()).ToList());
    }
}
