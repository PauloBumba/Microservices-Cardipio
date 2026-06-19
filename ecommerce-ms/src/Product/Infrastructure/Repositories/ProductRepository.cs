using Microsoft.EntityFrameworkCore;
using Product.Application.Repositories;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Repositories;

public sealed class ProductRepository(ProductDbContext db) : IProductRepository
{
    public async Task<Productss?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Products.FindAsync([id], ct);

    public async Task<Productss?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        await db.Products.FirstOrDefaultAsync(p => p.Sku == sku.ToUpperInvariant(), ct);

    public async Task<List<Productss>> GetAllAsync(CancellationToken ct = default) =>
        await db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<List<Productss>> GetByCategoryAsync(string category, CancellationToken ct = default) =>
        await db.Products.Where(p => p.IsActive && p.Category == category).ToListAsync(ct);

    public async Task AddAsync(Productss product, CancellationToken ct = default) =>
        await db.Products.AddAsync(product, ct);

    public async Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default) =>
        await db.Products.AnyAsync(p => p.Sku == sku.ToUpperInvariant(), ct);
}
