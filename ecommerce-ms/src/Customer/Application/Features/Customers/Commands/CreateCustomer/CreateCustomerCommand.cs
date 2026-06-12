using Customer.Application.DTOs;
using MediatR;
namespace Customer.Application.Features.Customers.Commands.CreateCustomer;
public sealed record CreateCustomerCommand(
    string Name, string Email, string Phone,
    string Street, string City, string State, string ZipCode, string Country) : IRequest<CustomerDto>;
