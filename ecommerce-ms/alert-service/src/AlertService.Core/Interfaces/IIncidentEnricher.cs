using AlertService.Core.Models;

namespace AlertService.Core.Interfaces;

/// <summary>
/// Orquestra o enriquecimento completo de um incidente:
/// coleta logs (Loki), métricas (Prometheus), correlaciona e chama AI.
/// </summary>
public interface IIncidentEnricher
{
    Task<IncidentContext> EnrichAsync(GrafanaWebhookPayload payload, CancellationToken ct = default);
}
