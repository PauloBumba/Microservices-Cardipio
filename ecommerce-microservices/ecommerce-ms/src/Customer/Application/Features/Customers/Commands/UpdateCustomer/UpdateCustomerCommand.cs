using Customer.Application.DTOs;
using MediatR;
namespace Customer.Application.Features.Customers.Commands.UpdateCustomer;
public sealed record UpdateCustomerCommand(
    Guid Id, string Name, string Phone,
    string Street, string City, string State, string ZipCode, string Country) : IRequest<CustomerDto>;
