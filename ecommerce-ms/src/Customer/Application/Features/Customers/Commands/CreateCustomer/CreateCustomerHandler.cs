using Customer.Application.Repositories;
using Customer.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerHandler(
    ICustomerRepository repo,
    ILogger<CreateCustomerHandler> logger)
    : IRequestHandler<CreateCustomerCommand, ApiResponse<Guid>>
{
    public async Task<ApiResponse<Guid>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        if (await repo.EmailExistsAsync(cmd.Email, ct))
            return ApiResponse<Guid>.Fail($"E-mail '{cmd.Email}' já cadastrado.");

        var customer = Customerss.Create(
            cmd.Name, cmd.Email, cmd.Phone,
            cmd.Street, cmd.City, cmd.State, cmd.ZipCode, cmd.Country);

        await repo.AddAsync(customer, ct);

        // ✅ NÃO chame CommitAsync aqui — o TransactionBehavior faz isso automaticamente
        logger.LogInformation("Cliente criado: {Id}", customer.Id);
        return ApiResponse<Guid>.Ok(customer.Id);
    }
}
