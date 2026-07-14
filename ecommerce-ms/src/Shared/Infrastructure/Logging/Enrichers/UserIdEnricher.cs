using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class UserIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
        }
    }
}
