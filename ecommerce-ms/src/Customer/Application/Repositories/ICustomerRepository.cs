using Customer.Domain.Entities;

namespace Customer.Application.Repositories;
public interface ICustomerRepository
{
    Task<Customerss?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customerss?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<Customerss?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<Customerss>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(Customerss customer, CancellationToken ct = default);
}
