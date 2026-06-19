using Customer.Application.DTOs;
using Customer.Application.Mappings;
using Customer.Application.Repositories;
using MediatR;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdHandler(ICustomerRepository repo)
    : IRequestHandler<GetCustomerByIdQuery, ApiResponse<CustomerDto>>
{
    public async Task<ApiResponse<CustomerDto>> Handle(GetCustomerByIdQuery query, CancellationToken ct)
    {
        var customer = await repo.GetByIdAsync(query.Id, ct);
        return customer is null
            ? ApiResponse<CustomerDto>.Fail("Cliente não encontrado.")
            : ApiResponse<CustomerDto>.Ok(customer.ToDto());
    }
}
