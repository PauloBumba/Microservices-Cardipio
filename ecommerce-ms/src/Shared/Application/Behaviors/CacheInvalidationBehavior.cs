using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.Application.Response;

namespace Shared.Application.Behaviors;

/// <summary>
/// Implemente ICacheInvalidator no Command para invalidar chaves após sucesso.
/// </summary>
public interface ICacheInvalidator
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
}

public sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        if (request is ICacheInvalidator inv
            && response is IApiResponseMarker { IsSuccess: true })
        {
            foreach (var key in inv.CacheKeysToInvalidate)
            {
                await cache.RemoveAsync(key, ct);
                logger.LogDebug("[Cache] INVALIDATED {Key}", key);
            }
        }

        return response;
    }
}
