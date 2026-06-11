namespace Order.Application.Contracts;
public interface IProductServiceClient
{
    Task<ProductInfo?> GetProductAsync(Guid productId, CancellationToken ct = default);
    Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken ct = default);
}
public sealed record ProductInfo(
    Guid Id, string Name, string Sku, decimal Price, string Currency, int AvailableQuantity, bool IsActive);
