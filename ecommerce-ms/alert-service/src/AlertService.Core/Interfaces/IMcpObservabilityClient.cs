using AlertService.Core.Models;

namespace AlertService.Core.Interfaces;

/// <summary>
/// Cliente MCP para o servidor grafana/mcp-grafana (Loki + Prometheus via Grafana).
/// </summary>
public interface IMcpObservabilityClient
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LogEntry>> QueryLokiLogsAsync(
        string service,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit = 100,
        CancellationToken ct = default);

    Task<ServiceMetricsSnapshot?> GetServiceSnapshotAsync(
        string service,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
