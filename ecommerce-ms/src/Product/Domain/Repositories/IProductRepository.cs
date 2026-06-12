using Product.Domain.Entities;
namespace Product.Domain.Repositories;
public interface IProductRepository
{
    Task<Productss?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Productss?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<Productss?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<IEnumerable<Productss>> GetPagedAsync(int page, int size, string? category, CancellationToken ct = default);
    Task<int> CountAsync(string? category, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
    Task AddAsync(Productss product, CancellationToken ct = default);
}
