using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using AlertService.Core.Services;
using Microsoft.Extensions.Logging;

namespace AlertService.Core.Services;

/// <summary>
/// Orquestra o pipeline:
///   payload → [MCP ou HTTP: Loki + Prometheus] → Correlate → Anomaly → Ollama AI
/// </summary>
public sealed class IncidentEnricher(
    ILokiClient              lokiClient,
    IPrometheusClient        prometheusClient,
    IMcpObservabilityClient  mcpClient,
    IIncidentAiService       aiService,
    IRecentIncidentStore     incidentStore,
    FeatureFlags             flags,
    ILogger<IncidentEnricher> logger) : IIncidentEnricher
{
    public async Task<IncidentContext> EnrichAsync(
        GrafanaWebhookPayload payload,
        CancellationToken ct = default)
    {
        var first      = payload.AlertsList.FirstOrDefault();
        var service    = ExtractService(first?.Labels);
        var firedAt    = first?.StartsAt ?? DateTimeOffset.UtcNow;
        var incidentId = $"{service}-{firedAt:yyyyMMddHHmmss}-{Guid.NewGuid():N[..6]}";

        var context = new IncidentContext
        {
            IncidentId      = incidentId,
            OriginalPayload = payload,
            Service         = service,
            AlertFiredAt    = firedAt
        };

        IReadOnlyList<LogEntry> logs = [];
        ServiceMetricsSnapshot? metrics = null;
        string? enrichmentSource = null;

        // ── Fase 1+2: MCP (prioridade) ou HTTP direto ─────────────────────────
        if (flags.McpEnrichmentEnabled)
        {
            logs = await SafeExecuteAsync(
                () => mcpClient.QueryLokiLogsAsync(service, context.WindowStart, context.WindowEnd, ct: ct),
                fallback: [],
                phase: "MCP-Loki",
                ct: ct);

            metrics = await SafeExecuteAsync(
                () => mcpClient.GetServiceSnapshotAsync(service, context.WindowStart, context.WindowEnd, ct),
                fallback: null,
                phase: "MCP-Prometheus",
                ct: ct);

            if (logs.Count > 0 || metrics is not null)
                enrichmentSource = "mcp";
        }

        if (enrichmentSource is null && flags.LokiEnrichmentEnabled)
        {
            logs = await SafeExecuteAsync(
                () => lokiClient.QueryRangeAsync(service, context.WindowStart, context.WindowEnd, ct: ct),
                fallback: logs,
                phase: "Loki",
                ct: ct);
            enrichmentSource ??= logs.Count > 0 ? "http" : null;
        }

        if (enrichmentSource is null && flags.PrometheusEnrichmentEnabled)
        {
            metrics = await SafeExecuteAsync(
                () => prometheusClient.GetServiceSnapshotAsync(service, context.WindowStart, context.WindowEnd, ct),
                fallback: metrics,
                phase: "Prometheus",
                ct: ct);
            enrichmentSource ??= metrics is not null ? "http" : null;
        }

        // ── Fase 3: Correlação ────────────────────────────────────────────────
        var timeline  = BuildTimeline(logs, metrics, payload, firedAt);
        var anomalies = DetectAnomalies(metrics);

        context = context with
        {
            Logs             = logs,
            Metrics          = metrics,
            Timeline         = timeline,
            Anomalies        = anomalies,
            EnrichmentSource = enrichmentSource
        };

        // ── Fase 4: AI (Ollama) ───────────────────────────────────────────────
        if (flags.AiEnabled)
        {
            var analysis = await SafeExecuteAsync(
                () => aiService.AnalyzeAsync(context, ct),
                fallback: null,
                phase: "OllamaAI",
                ct: ct);

            context = context with { AiAnalysis = analysis };
        }

        incidentStore.Add(context);

        logger.LogInformation(
            "[{IncidentId}] Enriquecimento concluído: source={Source}, {LogCount} logs, AI={AiEnabled}",
            incidentId, enrichmentSource ?? "none", logs.Count, flags.AiEnabled);

        return context;
    }

    internal static string ExtractService(Dictionary<string, string>? labels) =>
        labels?.GetValueOrDefault("service")
        ?? labels?.GetValueOrDefault("job")
        ?? "unknown";

    // ─── Correlation Engine ────────────────────────────────────────────────────

    private static IReadOnlyList<CorrelatedEvent> BuildTimeline(
        IReadOnlyList<LogEntry>       logs,
        ServiceMetricsSnapshot?       metrics,
        GrafanaWebhookPayload         payload,
        DateTimeOffset                alertFiredAt)
    {
        var events = new List<CorrelatedEvent>();

        foreach (var log in logs.Where(l => l.Level is "ERROR" or "WARN" or "WARNING"))
        {
            events.Add(new CorrelatedEvent(
                Timestamp:   log.Timestamp,
                Type:        log.Level is "ERROR" ? CorrelatedEventType.LogError : CorrelatedEventType.LogWarn,
                Source:      "loki",
                Description: TruncateMessage(log.Message, 120),
                Severity:    log.Level is "ERROR" ? Severity.High : Severity.Medium
            ));
        }

        if (metrics?.ErrorRatePercent is > 5)
        {
            events.Add(new CorrelatedEvent(
                Timestamp:   alertFiredAt.AddMinutes(-1),
                Type:        CorrelatedEventType.MetricSpike,
                Source:      "prometheus",
                Description: $"Error rate em {metrics.ErrorRatePercent:F1}% (limiar: 5%)",
                Severity:    metrics.ErrorRatePercent > 20 ? Severity.Critical : Severity.High
            ));
        }

        if (metrics?.LatencyP99Ms is > 1000)
        {
            events.Add(new CorrelatedEvent(
                Timestamp:   alertFiredAt.AddMinutes(-2),
                Type:        CorrelatedEventType.MetricSpike,
                Source:      "prometheus",
                Description: $"Latência p99 em {metrics.LatencyP99Ms:F0}ms",
                Severity:    Severity.High
            ));
        }

        events.Add(new CorrelatedEvent(
            Timestamp:   alertFiredAt,
            Type:        CorrelatedEventType.AlertFired,
            Source:      "grafana",
            Description: payload.Title,
            Severity:    Severity.Critical
        ));

        return events.OrderBy(e => e.Timestamp).ToList().AsReadOnly();
    }

    private static IReadOnlyList<Anomaly> DetectAnomalies(ServiceMetricsSnapshot? metrics)
    {
        if (metrics is null) return [];

        var anomalies = new List<Anomaly>();

        Check(anomalies, "error_rate_percent",  metrics.ErrorRatePercent,  baseline: 1.0,  stdDev: 1.5);
        Check(anomalies, "latency_p99_ms",      metrics.LatencyP99Ms,      baseline: 200,  stdDev: 100);
        Check(anomalies, "cpu_usage_percent",   metrics.CpuUsagePercent,   baseline: 40,   stdDev: 20);
        Check(anomalies, "memory_usage_mb",     metrics.MemoryUsageMb,     baseline: 512,  stdDev: 128);

        return anomalies.AsReadOnly();

        static void Check(List<Anomaly> list, string name, double? observed, double baseline, double stdDev)
        {
            if (observed is null) return;
            var zScore = (observed.Value - baseline) / stdDev;
            if (Math.Abs(zScore) > 2.0)
            {
                list.Add(new Anomaly(
                    MetricName:    name,
                    ObservedValue: observed.Value,
                    BaselineValue: baseline,
                    ZScore:        zScore,
                    Description:   $"{name} em {observed.Value:F2} (baseline {baseline:F2}, z={zScore:F2})"
                ));
            }
        }
    }

    private async Task<T> SafeExecuteAsync<T>(
        Func<Task<T>> action,
        T fallback,
        string phase,
        CancellationToken ct)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[{Phase}] Timeout — usando fallback", phase);
            return fallback;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Phase}] Falha — usando fallback", phase);
            return fallback;
        }
    }

    private static string TruncateMessage(string msg, int max) =>
        msg.Length <= max ? msg : string.Concat(msg.AsSpan(0, max), "…");
}
