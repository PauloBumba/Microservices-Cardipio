using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.Application.Caching;
using System.Text.Json;

namespace Shared.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery cq)
            return await next();

        var key = cq.CacheKey;
        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null)
        {
            logger.LogDebug("[Cache] HIT {Key}", key);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        var response = await next();
        var expiry = cq.CacheDuration ?? DefaultExpiry;
        await cache.SetStringAsync(key, JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry }, ct);
        logger.LogDebug("[Cache] SET {Key} (expiry {Expiry})", key, expiry);
        return response;
    }
}
