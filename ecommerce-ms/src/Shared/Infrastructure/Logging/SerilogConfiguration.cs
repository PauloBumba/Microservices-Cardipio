using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.Grafana.Loki;
using Shared.Infrastructure.Logging.Enrichers;
using Shared.Infrastructure.Logging.Filters;

namespace Shared.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        var logPath = Path.Combine(builder.Environment.ContentRootPath, "Logs");
        
        // Criar diretórios de log
        Directory.CreateDirectory(Path.Combine(logPath, "Application"));
        Directory.CreateDirectory(Path.Combine(logPath, "Audit"));
        Directory.CreateDirectory(Path.Combine(logPath, "Security"));
        Directory.CreateDirectory(Path.Combine(logPath, "Background"));

        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration)
              .MinimumLevel.Information()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
              .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithProperty("Service", ctx.HostingEnvironment.ApplicationName)
              .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
              .Enrich.With(new TraceIdEnricher())
              .Enrich.With(new CorrelationIdEnricher())
              .Enrich.With(new EnvironmentEnricher(ctx.HostingEnvironment.EnvironmentName))
              .Filter.With(new SensitiveDataFilter())
              .WriteTo.Console(new JsonFormatter())
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Application", "application-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30))
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Audit", "audit-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 365))
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Security", "security-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 365))
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Background", "background-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30));

            // Configurar Loki se disponível
            var lokiUrl = ctx.Configuration["Loki:Url"];
            if (!string.IsNullOrEmpty(lokiUrl))
            {
                lc.WriteTo.GrafanaLoki(lokiUrl, labels: new[]
                {
                    new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "service", Value = ctx.HostingEnvironment.ApplicationName },
                    new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "environment", Value = ctx.HostingEnvironment.EnvironmentName }
                });
            }
        });
    }
}
