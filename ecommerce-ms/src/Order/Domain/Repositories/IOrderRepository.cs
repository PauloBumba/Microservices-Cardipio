using Order.Domain.Entities;
namespace Order.Domain.Repositories;
public interface IOrderRepository
{
    Task<Orders?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Orders?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Orders>> GetByCustomerAsync(Guid customerId, int page, int size, CancellationToken ct = default);
    Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<Orders>> GetPagedAsync(int page, int size, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(Orders order, CancellationToken ct = default);
}
