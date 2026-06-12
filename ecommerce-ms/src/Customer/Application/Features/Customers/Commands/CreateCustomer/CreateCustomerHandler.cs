using Customer.Application.DTOs;
using Customer.Application.Mappings;

using Customer.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Customer.Domain.Entities;

namespace Customer.Application.Features.Customers.Commands.CreateCustomer
{
    public sealed class CreateCustomerHandler(
        ICustomerRepository repo, IUnitOfWork uow, ILogger<CreateCustomerHandler> log)
        : IRequestHandler<CreateCustomerCommand, CustomerDto>
    {
        public async Task<CustomerDto> Handle(CreateCustomerCommand cmd, CancellationToken ct)
        {
            if (await repo.EmailExistsAsync(cmd.Email, ct))
                throw new InvalidOperationException($"E-mail '{cmd.Email}' já cadastrado.");
            var c = Customerss.Create(cmd.Name, cmd.Email, cmd.Phone, cmd.Street, cmd.City, cmd.State, cmd.ZipCode, cmd.Country);
            await repo.AddAsync(c, ct);
            await uow.CommitAsync(ct);
            log.LogInformation("Cliente criado: {Id}", c.Id);
            return c.ToDto();


        }
    }
}