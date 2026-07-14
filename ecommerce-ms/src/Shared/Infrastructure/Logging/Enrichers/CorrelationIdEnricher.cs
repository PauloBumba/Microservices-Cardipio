using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (LogContext.TryGet("CorrelationId", out var correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        }
    }
}
