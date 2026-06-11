using Microsoft.Extensions.Logging;
using Order.Application.Contracts;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
namespace Order.Infrastructure.Http;
public sealed class ProductServiceClient(HttpClient http, ILogger<ProductServiceClient> logger) : IProductServiceClient
{
    public async Task<ProductInfo?> GetProductAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync($"api/v1/products/{productId}", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<ProductInfo>(cancellationToken: ct);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Falha ao consultar produto {Id}", productId); throw; }
    }
    public async Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { Quantity = quantity });
        var resp = await http.PostAsync($"api/v1/products/{productId}/stock/reserve",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);
        return resp.IsSuccessStatusCode;
    }
}
