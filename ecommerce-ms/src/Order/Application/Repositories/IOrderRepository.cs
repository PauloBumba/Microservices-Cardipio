using Order.Domain.Entities;

namespace Order.Application.Repositories;

public interface IOrderRepository
{
    Task<Orders?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Orders?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<List<Orders>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<List<Orders>> GetByCustomerAsync(Guid customerId, int page, int size, CancellationToken ct = default);
    Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<List<Orders>> GetPagedAsync(int page, int size, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(Orders order, CancellationToken ct = default);
}
