namespace AlertService.Infrastructure.MCP;

public sealed class McpOptions
{
    /// <summary>URL base do grafana/mcp-grafana (streamable-http).</summary>
    public string GrafanaMcpBaseUrl { get; set; } = "http://mcp-grafana:8000";

    public string LokiDatasourceUid { get; set; } = "loki";
    public string PrometheusDatasourceUid { get; set; } = "prometheus";
    public int TimeoutSec { get; set; } = 15;
}
