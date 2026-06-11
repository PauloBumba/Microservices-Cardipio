using Customer.Application.DTOs;
using Customer.Application.Mappings;
using Customer.Domain.Exceptions;
using Customer.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Customer.Application.Features.Customers.Commands.UpdateCustomer;
public sealed class UpdateCustomerHandler(
    ICustomerRepository repo, IUnitOfWork uow, ILogger<UpdateCustomerHandler> log)
    : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var c = await repo.GetByIdTrackedAsync(cmd.Id, ct) ?? throw new CustomerNotFoundException(cmd.Id);
        c.Update(cmd.Name,cmd.Phone,cmd.Street,cmd.City,cmd.State,cmd.ZipCode,cmd.Country);
        await uow.CommitAsync(ct);
        log.LogInformation("Cliente atualizado: {Id}", c.Id);
        return c.ToDto();
    }
}
