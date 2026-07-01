using AlertService.Core.Models;

namespace AlertService.Core.Interfaces;

public interface IPrometheusClient
{
    /// <summary>
    /// Executa uma query range no Prometheus.
    /// </summary>
    Task<IReadOnlyList<MetricSample>> QueryRangeAsync(
        string promQl,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeSpan step,
        CancellationToken ct = default);

    /// <summary>
    /// Atalho: busca métricas padrão de um serviço (error_rate, latency p99, cpu).
    /// </summary>
    Task<ServiceMetricsSnapshot> GetServiceSnapshotAsync(
        string service,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
