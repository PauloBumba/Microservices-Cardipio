using AlertService.Core.Models;

namespace AlertService.Core.Interfaces;

public interface IRecentIncidentStore
{
    void Add(IncidentContext context);
    IReadOnlyList<IncidentContext> GetRecent(int count = 10);
}
