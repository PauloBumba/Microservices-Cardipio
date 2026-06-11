using Customer.Application.DTOs;
using MediatR;
namespace Customer.Application.Features.Customers.Queries.GetCustomers;
public sealed record GetCustomersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<CustomerDto>>;
