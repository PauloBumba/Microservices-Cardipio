using Microsoft.EntityFrameworkCore;
using Order.Domain.Repositories;
using Order.Infrastructure.Persistence;
namespace Order.Infrastructure.Repositories;
public sealed class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public async Task<Order.Domain.Entities.Orderss?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
    public async Task<Order.Domain.Entities.Orderss?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
    public async Task<IEnumerable<Order.Domain.Entities.Orderss>> GetByCustomerAsync(
        Guid customerId, int page, int size, CancellationToken ct = default)
        => await db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page-1)*size).Take(size).ToListAsync(ct);
    public async Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => await db.Orders.CountAsync(o => o.CustomerId == customerId, ct);
    public async Task<IEnumerable<Order.Domain.Entities.Orderss>> GetPagedAsync(int page, int size, CancellationToken ct = default)
        => await db.Orders.AsNoTracking().Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page-1)*size).Take(size).ToListAsync(ct);
    public async Task<int> CountAsync(CancellationToken ct = default) => await db.Orders.CountAsync(ct);
    public async Task AddAsync(Order.Domain.Entities.Orderss order, CancellationToken ct = default)
        => await db.Orders.AddAsync(order, ct);
}
