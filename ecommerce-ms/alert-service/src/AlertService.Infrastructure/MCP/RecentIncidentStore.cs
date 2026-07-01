using System.Collections.Concurrent;
using AlertService.Core.Interfaces;
using AlertService.Core.Models;

namespace AlertService.Infrastructure.MCP;

public sealed class RecentIncidentStore : IRecentIncidentStore
{
    private const int MaxItems = 50;
    private readonly ConcurrentQueue<IncidentContext> _items = new();

    public void Add(IncidentContext context)
    {
        _items.Enqueue(context);
        while (_items.Count > MaxItems && _items.TryDequeue(out _)) { }
    }

    public IReadOnlyList<IncidentContext> GetRecent(int count = 10) =>
        _items.Reverse().Take(count).ToList();
}
