using Order.Domain.Entities;

namespace Order.Application.Repositories;

public interface IOrderRepository
{
    Task<Orders?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Orders?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<List<Orders>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(Orders order, CancellationToken ct = default);
}
