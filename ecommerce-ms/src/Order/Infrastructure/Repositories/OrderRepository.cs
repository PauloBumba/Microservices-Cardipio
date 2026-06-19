using Microsoft.EntityFrameworkCore;
using Order.Application.Repositories;
using Order.Domain.Entities;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.Repositories;

public sealed class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public async Task<Orders?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Orders?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default) =>
        await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

    public async Task<List<Orders>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        await db.Orders.Include(o => o.Items).Where(o => o.CustomerId == customerId).ToListAsync(ct);

    public async Task AddAsync(Orders order, CancellationToken ct = default) =>
        await db.Orders.AddAsync(order, ct);
}
