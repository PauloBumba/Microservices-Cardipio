using Customer.Application.Repositories;
using MediatR;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Commands.DeactivateCustomer;

public sealed class DeactivateCustomerHandler(ICustomerRepository repo)
    : IRequestHandler<DeactivateCustomerCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeactivateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await repo.GetByIdAsync(cmd.Id, ct);
        if (customer is null) return ApiResponse<bool>.Fail("Cliente não encontrado.");

        customer.Deactivate();
        return ApiResponse<bool>.Ok(true);
    }
}
