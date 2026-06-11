using Customer.Application.DTOs;
using MediatR;
namespace Customer.Application.Features.Customers.Queries.GetCustomerById;
public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto?>;
