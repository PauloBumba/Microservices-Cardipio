using Order.Domain.Entities;
namespace Order.Domain.Repositories;
public interface IOrderRepository
{
    Task<Orderss?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Orderss?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Orderss>> GetByCustomerAsync(Guid customerId, int page, int size, CancellationToken ct = default);
    Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<Orderss>> GetPagedAsync(int page, int size, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(Orderss order, CancellationToken ct = default);
}
