using System.ComponentModel;
using System.Text.Json;
using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using AlertService.Core.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AlertService.Infrastructure.MCP;

/// <summary>
/// Expõe o alert-service como servidor MCP para agentes externos (Cursor, Claude, etc.).
/// </summary>
[McpServerToolType]
public sealed class IncidentMcpTools(
    IMcpObservabilityClient mcpClient,
    IRecentIncidentStore incidentStore,
    FeatureFlags flags,
    ILogger<IncidentMcpTools> logger)
{
    [McpServerTool, Description("Lista os últimos incidentes enriquecidos pelo alert-service.")]
    public string ListRecentIncidents(
        [Description("Quantidade máxima de incidentes (default 10)")] int count = 10)
    {
        var incidents = incidentStore.GetRecent(Math.Clamp(count, 1, 50));
        if (incidents.Count == 0)
            return "Nenhum incidente registrado ainda.";

        var payload = incidents.Select(i => new
        {
            i.IncidentId,
            i.Service,
            i.AlertFiredAt,
            i.EnrichmentSource,
            LogCount = i.Logs.Count,
            AiSummary = i.AiAnalysis?.HumanSummary,
            RootCause = i.AiAnalysis?.RootCause
        });

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Investiga um serviço consultando Loki e Prometheus via MCP (Grafana).")]
    public async Task<string> InvestigateService(
        [Description("Nome do serviço Docker Compose, ex: customer-service")] string service,
        CancellationToken cancellationToken = default)
    {
        if (!flags.McpEnrichmentEnabled && !flags.McpServerEnabled)
            return "MCP desabilitado. Ative FeatureFlags:McpEnrichmentEnabled ou McpServerEnabled.";

        var to   = DateTimeOffset.UtcNow;
        var from = to.AddMinutes(-10);

        try
        {
            var logs    = await mcpClient.QueryLokiLogsAsync(service, from, to, 50, cancellationToken);
            var metrics = await mcpClient.GetServiceSnapshotAsync(service, from, to, cancellationToken);

            var result = new
            {
                service,
                window = new { from, to },
                logs = logs.Take(20).Select(l => new { l.Timestamp, l.Level, Message = Truncate(l.Message, 200) }),
                metrics,
                logCount = logs.Count
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MCP Server] InvestigateService falhou para {Service}", service);
            return $"Erro na investigação: {ex.Message}";
        }
    }

    [McpServerTool, Description("Verifica conectividade com o servidor grafana/mcp-grafana.")]
    public async Task<string> CheckMcpHealth(CancellationToken cancellationToken = default)
    {
        var ok = await mcpClient.IsAvailableAsync(cancellationToken);
        return ok
            ? "MCP Grafana OK — grafana/mcp-grafana acessível."
            : "MCP Grafana indisponível — verifique o container mcp-grafana.";
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : string.Concat(s.AsSpan(0, max), "…");
}
