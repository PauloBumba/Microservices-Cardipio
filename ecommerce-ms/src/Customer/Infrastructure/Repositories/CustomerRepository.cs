using Customer.Domain.Repositories;
using Customer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Customer.Infrastructure.Repositories;
public sealed class CustomerRepository(CustomerDbContext db) : ICustomerRepository
{
    public async Task<Domain.Entities.Customerss?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<Domain.Entities.Customerss?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<Domain.Entities.Customerss?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Email.Value == email.ToLowerInvariant(), ct);
    public async Task<IEnumerable<Domain.Entities.Customerss>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => await db.Customers.AsNoTracking().OrderBy(c => c.Name).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
    public async Task<int> CountAsync(CancellationToken ct = default) => await db.Customers.CountAsync(ct);
    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await db.Customers.AnyAsync(c => c.Email.Value == email.ToLowerInvariant(), ct);
    public async Task AddAsync(Domain.Entities.Customerss customer, CancellationToken ct = default)
        => await db.Customers.AddAsync(customer, ct);
}
