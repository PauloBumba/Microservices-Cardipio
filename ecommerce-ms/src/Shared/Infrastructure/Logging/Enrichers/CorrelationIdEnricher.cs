using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // LogContext não tem TryGet, então vamos tentar obter da propriedade se já existir
        if (logEvent.Properties.TryGetValue("CorrelationId", out var correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        }
    }
}
