using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Product.Application.DTOs;
using Product.Application.Mappings;
using Product.Domain.Repositories;
using System.Text.Json;
namespace Product.Application.Features.Products.Queries.GetProductById;
public sealed class GetProductByIdHandler(
    IProductRepository repo, IDistributedCache cache, ILogger<GetProductByIdHandler> log)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery q, CancellationToken ct)
    {
        var key = $"product:{q.Id}";
        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null) { log.LogDebug("Cache hit: {Key}", key); return JsonSerializer.Deserialize<ProductDto>(cached); }
        var p = await repo.GetByIdAsync(q.Id, ct);
        if (p is null) return null;
        var dto = p.ToDto();
        await cache.SetStringAsync(key, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, ct);
        return dto;
    }
}
