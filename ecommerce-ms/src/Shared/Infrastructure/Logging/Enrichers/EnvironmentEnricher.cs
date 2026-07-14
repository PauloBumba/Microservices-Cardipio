using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class EnvironmentEnricher : ILogEventEnricher
{
    private readonly string _environment;

    public EnvironmentEnricher(string environment)
    {
        _environment = environment;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", _environment));
    }
}
