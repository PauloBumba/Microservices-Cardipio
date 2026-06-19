using Customer.Application.Repositories;
using MediatR;
using Shared.Application.Response;

namespace Customer.Application.Features.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerHandler(ICustomerRepository repo)
    : IRequestHandler<UpdateCustomerCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await repo.GetByIdAsync(cmd.Id, ct);
        if (customer is null) return ApiResponse<bool>.Fail("Cliente não encontrado.");

        customer.Update(cmd.Name, cmd.Phone, cmd.Street, cmd.City, cmd.State, cmd.ZipCode, cmd.Country);
        return ApiResponse<bool>.Ok(true);
    }
}
