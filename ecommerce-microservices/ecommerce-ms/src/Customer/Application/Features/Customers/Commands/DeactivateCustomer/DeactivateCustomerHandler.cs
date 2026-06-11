using Customer.Domain.Exceptions;
using Customer.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Customer.Application.Features.Customers.Commands.DeactivateCustomer;
public sealed class DeactivateCustomerHandler(
    ICustomerRepository repo, IUnitOfWork uow, ILogger<DeactivateCustomerHandler> log)
    : IRequestHandler<DeactivateCustomerCommand>
{
    public async Task Handle(DeactivateCustomerCommand cmd, CancellationToken ct)
    {
        var c = await repo.GetByIdTrackedAsync(cmd.Id, ct) ?? throw new CustomerNotFoundException(cmd.Id);
        c.Deactivate();
        await uow.CommitAsync(ct);
        log.LogInformation("Cliente desativado: {Id}", c.Id);
    }
}
