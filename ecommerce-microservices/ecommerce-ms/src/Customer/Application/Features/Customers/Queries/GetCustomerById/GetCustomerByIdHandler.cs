using Customer.Application.DTOs;
using Customer.Application.Mappings;
using Customer.Domain.Repositories;
using MediatR;
namespace Customer.Application.Features.Customers.Queries.GetCustomerById;
public sealed class GetCustomerByIdHandler(ICustomerRepository repo) : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery q, CancellationToken ct)
        => (await repo.GetByIdAsync(q.Id, ct))?.ToDto();
}
