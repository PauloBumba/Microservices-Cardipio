namespace Shared.Application.Caching;

/// <summary>
/// Implemente em Queries que devem ser cacheadas.
/// O CachingBehavior lê/grava automaticamente.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? CacheDuration => null; // null = usa o padrão (5 min)
}
