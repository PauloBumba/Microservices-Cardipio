namespace AlertService.Core.Models;

// ─── Contexto completo de um incidente enriquecido ────────────────────────────

public sealed record IncidentContext
{
    public required string IncidentId { get; init; }
    public required GrafanaWebhookPayload OriginalPayload { get; init; }
    public required string Service { get; init; }
    public required DateTimeOffset AlertFiredAt { get; init; }
    public DateTimeOffset WindowStart => AlertFiredAt.AddMinutes(-5);
    public DateTimeOffset WindowEnd   => AlertFiredAt.AddMinutes(5);

    public IReadOnlyList<LogEntry>       Logs    { get; init; } = [];
    public ServiceMetricsSnapshot?       Metrics { get; init; }
    public IReadOnlyList<CorrelatedEvent> Timeline { get; init; } = [];
    public IReadOnlyList<Anomaly>        Anomalies { get; init; } = [];
    public AiAnalysis?                   AiAnalysis { get; init; }
    /// <summary>"http" | "mcp" | null quando legacy.</summary>
    public string?                       EnrichmentSource { get; init; }
}

// ─── Loki ─────────────────────────────────────────────────────────────────────

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string         Level,     // INFO, WARN, ERROR, etc
    string         Message,
    string?        TraceId = null,
    string?        SpanId  = null
);

// ─── Prometheus ───────────────────────────────────────────────────────────────

public sealed record MetricSample(
    DateTimeOffset Timestamp,
    string         MetricName,
    double         Value,
    IReadOnlyDictionary<string, string> Labels
);

public sealed record ServiceMetricsSnapshot
{
    public string  Service          { get; init; } = "";
    public double? ErrorRatePercent { get; init; }    // % de requests com 5xx
    public double? LatencyP99Ms     { get; init; }    // p99 latência em ms
    public double? CpuUsagePercent  { get; init; }
    public double? MemoryUsageMb    { get; init; }
    public double? RequestRateRpm   { get; init; }    // requests por minuto
    public IReadOnlyList<MetricSample> RawSamples { get; init; } = [];
}

// ─── Correlation Engine ───────────────────────────────────────────────────────

public sealed record CorrelatedEvent(
    DateTimeOffset Timestamp,
    CorrelatedEventType Type,
    string         Source,      // "loki", "prometheus", "grafana"
    string         Description,
    Severity       Severity
);

public enum CorrelatedEventType { LogError, LogWarn, MetricSpike, MetricAnomaly, AlertFired }
public enum Severity             { Low, Medium, High, Critical }

// ─── Anomaly Detection ────────────────────────────────────────────────────────

public sealed record Anomaly(
    string MetricName,
    double ObservedValue,
    double BaselineValue,
    double ZScore,
    string Description
);

// ─── AI Analysis ─────────────────────────────────────────────────────────────

public sealed record AiAnalysis
{
    public required string RootCause        { get; init; }
    public required string HumanSummary     { get; init; }
    public required AiSeverity Severity     { get; init; }
    public required string ProbableCause    { get; init; }   // "deploy", "db", "overload", "network", etc
    public AlertDecision OperationalDecision { get; init; } = AlertDecision.Notify;
    public double Confidence { get; init; } = 0.5;
    public string Impact { get; init; } = "Impacto ainda nao estimado";
    public IReadOnlyList<string> Evidence { get; init; } = [];
    public IReadOnlyList<string> Suggestions { get; init; } = [];
    public required DateTimeOffset GeneratedAt { get; init; }
    public required string ModelUsed        { get; init; }
    public bool IsReliable                  { get; init; } = true; // false se timeout ou parse falhou
}

public enum AiSeverity { Low, Medium, High, Critical }

public enum AlertDecision { Ignore, Observe, Notify, Escalate }