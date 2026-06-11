using Customer.Application.DTOs;
using Customer.Application.Mappings;
using Customer.Domain.Repositories;
using MediatR;
namespace Customer.Application.Features.Customers.Queries.GetCustomers;
public sealed class GetCustomersHandler(ICustomerRepository repo) : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery q, CancellationToken ct)
    {
        var items = await repo.GetPagedAsync(q.Page, q.PageSize, ct);
        var total = await repo.CountAsync(ct);
        return new PagedResult<CustomerDto>(items.Select(c => c.ToDto()).ToList(), total, q.Page, q.PageSize);
    }
}
