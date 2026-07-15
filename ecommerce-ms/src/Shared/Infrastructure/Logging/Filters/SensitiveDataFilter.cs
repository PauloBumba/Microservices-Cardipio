using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Filters;

public class SensitiveDataFilter : ILogEventFilter
{
    private static readonly string[] SensitiveKeys = 
    {
        "password", "pwd", "secret", "token", "apikey", "creditcard", "ssn", "cpf",
        "senha", "segredo", "cartao", "cpf"
    };

    public bool IsEnabled(LogEvent logEvent)
    {
        SanitizeSensitiveData(logEvent);
        return true;
    }

    private void SanitizeSensitiveData(LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties.ToList())
        {
            var key = property.Key.ToLower();
            if (SensitiveKeys.Any(s => key.Contains(s)))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, new ScalarValue("***REDACTED***")));
            }
        }
    }
}
