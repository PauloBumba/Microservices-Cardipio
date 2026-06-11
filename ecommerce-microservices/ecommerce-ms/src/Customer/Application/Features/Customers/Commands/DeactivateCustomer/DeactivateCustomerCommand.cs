using MediatR;
namespace Customer.Application.Features.Customers.Commands.DeactivateCustomer;
public sealed record DeactivateCustomerCommand(Guid Id) : IRequest;
