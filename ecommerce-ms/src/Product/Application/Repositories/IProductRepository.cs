using Product.Domain.Entities;

namespace Product.Application.Repositories;

public interface IProductRepository
{
    Task<Productss?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Productss?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<List<Productss>> GetAllAsync(CancellationToken ct = default);
    Task<List<Productss>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task AddAsync(Productss product, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
}
