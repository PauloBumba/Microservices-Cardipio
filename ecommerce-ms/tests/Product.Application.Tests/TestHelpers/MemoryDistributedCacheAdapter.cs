using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Product.Application.Tests.TestHelpers;

/// <summary>
/// Adapter para usar IMemoryCache como IDistributedCache em testes.
/// Simplifica testes sem precisar de Redis real.
/// </summary>
public class MemoryDistributedCacheAdapter : IDistributedCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryDistributedCacheAdapter(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public byte[]? Get(string key)
    {
        return _memoryCache.Get<byte[]>(key);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return Task.FromResult(Get(key));
    }

    public void Refresh(string key)
    {
        // Não implementado para testes
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var expiry = options.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(5);
        _memoryCache.Set(key, value, expiry);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public string? GetString(string key)
    {
        var bytes = Get(key);
        return bytes == null ? null : Encoding.UTF8.GetString(bytes);
    }

    public Task<string?> GetStringAsync(string key, CancellationToken token = default)
    {
        return Task.FromResult(GetString(key));
    }

    public void SetString(string key, string value, DistributedCacheEntryOptions options)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Set(key, bytes, options);
    }

    public Task SetStringAsync(string key, string value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        SetString(key, value, options);
        return Task.CompletedTask;
    }
}
