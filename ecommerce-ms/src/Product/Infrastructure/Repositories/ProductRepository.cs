using Microsoft.EntityFrameworkCore;
using Product.Domain.Repositories;
using Product.Infrastructure.Persistence;
namespace Product.Infrastructure.Repositories;
public sealed class ProductRepository(ProductDbContext db) : IProductRepository
{
    public async Task<Domain.Entities.Productss?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task<Domain.Entities.Productss?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task<Domain.Entities.Productss?> GetBySkuAsync(string sku, CancellationToken ct = default)
        => await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku.ToUpperInvariant(), ct);
    public async Task<IEnumerable<Domain.Entities.Productss>> GetPagedAsync(int page, int size, string? cat, CancellationToken ct = default)
    {
        var q = db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(cat)) q = q.Where(p => p.Category == cat);
        return await q.OrderBy(p => p.Name).Skip((page-1)*size).Take(size).ToListAsync(ct);
    }
    public async Task<int> CountAsync(string? cat, CancellationToken ct = default)
    {
        var q = db.Products.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(cat)) q = q.Where(p => p.Category == cat);
        return await q.CountAsync(ct);
    }
    public async Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
        => await db.Products.AnyAsync(p => p.Sku == sku.ToUpperInvariant(), ct);
    public async Task AddAsync(Domain.Entities.Productss p, CancellationToken ct = default)
        => await db.Products.AddAsync(p, ct);
}
