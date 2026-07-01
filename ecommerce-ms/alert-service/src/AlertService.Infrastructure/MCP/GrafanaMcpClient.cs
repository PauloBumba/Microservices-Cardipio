using System.Globalization;
using System.Text.Json;
using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AlertService.Infrastructure.MCP;

/// <summary>
/// Conecta ao grafana/mcp-grafana via Streamable HTTP e expõe Loki + Prometheus como tools MCP.
/// </summary>
public sealed class GrafanaMcpClient(
    IOptions<McpOptions> options,
    ILogger<GrafanaMcpClient> logger) : IMcpObservabilityClient
{
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            await using var client = await CreateClientAsync(ct);
            await client.ListToolsAsync(cancellationToken: ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[MCP] grafana/mcp-grafana indisponível");
            return false;
        }
    }

    public async Task<IReadOnlyList<LogEntry>> QueryLokiLogsAsync(
        string service,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit = 100,
        CancellationToken ct = default)
    {
        var logql = $$"""{service="{{service}}"} | json | level=~"(?i)error|warn|warning" """;

        await using var client = await CreateClientAsync(ct);
        var result = await client.CallToolAsync(
            "query_loki_logs",
            new Dictionary<string, object?>
            {
                ["datasourceUid"] = options.Value.LokiDatasourceUid,
                ["logql"]         = logql,
                ["startRfc3339"]  = from.ToString("o"),
                ["endRfc3339"]    = to.ToString("o"),
                ["limit"]         = limit,
                ["direction"]     = "backward"
            },
            cancellationToken: ct);

        return ParseLokiResponse(ExtractText(result));
    }

    public async Task<ServiceMetricsSnapshot?> GetServiceSnapshotAsync(
        string service,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var start = from.ToString("o");
        var end   = to.ToString("o");
        const int step = 30;

        await using var client = await CreateClientAsync(ct);

        var errorRateTask = QueryScalarAsync(client,
            $$"""100 * sum(rate(http_requests_received_total{job="{{service}}",code=~"5.."}[5m])) / sum(rate(http_requests_received_total{job="{{service}}"}[5m]))""",
            start, end, step, ct);

        var latencyTask = QueryScalarAsync(client,
            $$"""histogram_quantile(0.99, sum by (le) (rate(http_request_duration_seconds_bucket{job="{{service}}"}[5m]))) * 1000""",
            start, end, step, ct);

        var cpuTask = QueryScalarAsync(client,
            $$"""rate(process_cpu_seconds_total{job="{{service}}"}[5m]) * 100""",
            start, end, step, ct);

        var memTask = QueryScalarAsync(client,
            $$"""process_working_set_bytes{job="{{service}}"} / 1024 / 1024""",
            start, end, step, ct);

        await Task.WhenAll(errorRateTask, latencyTask, cpuTask, memTask);

        return new ServiceMetricsSnapshot
        {
            Service          = service,
            ErrorRatePercent = await errorRateTask,
            LatencyP99Ms     = await latencyTask,
            CpuUsagePercent  = await cpuTask,
            MemoryUsageMb    = await memTask
        };
    }

    // ── MCP transport ─────────────────────────────────────────────────────────

    private async Task<McpClient> CreateClientAsync(CancellationToken ct)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint          = new Uri(options.Value.GrafanaMcpBaseUrl),
            TransportMode     = HttpTransportMode.AutoDetect,
            ConnectionTimeout = TimeSpan.FromSeconds(options.Value.TimeoutSec),
            Name              = "grafana-mcp"
        });

        return await McpClient.CreateAsync(transport, cancellationToken: ct);
    }

    private async Task<double?> QueryScalarAsync(
        McpClient client,
        string expr,
        string start,
        string end,
        int stepSeconds,
        CancellationToken ct)
    {
        try
        {
            var result = await client.CallToolAsync(
                "query_prometheus",
                new Dictionary<string, object?>
                {
                    ["datasourceUid"] = options.Value.PrometheusDatasourceUid,
                    ["expr"]          = expr,
                    ["startTime"]     = start,
                    ["endTime"]       = end,
                    ["stepSeconds"]   = stepSeconds,
                    ["queryType"]     = "range"
                },
                cancellationToken: ct);

            return ExtractPrometheusScalar(ExtractText(result));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MCP] Query Prometheus falhou: {Expr}", expr);
            return null;
        }
    }

    // ── Response parsers ──────────────────────────────────────────────────────

    private static string ExtractText(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(b => b.Text));

    private IReadOnlyList<LogEntry> ParseLokiResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                return ParseLokiDataArray(dataEl);

            // Resposta direta como array
            if (root.ValueKind == JsonValueKind.Array)
                return ParseLokiDataArray(root);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MCP] Falha ao parsear resposta Loki");
        }

        return [];
    }

    private static IReadOnlyList<LogEntry> ParseLokiDataArray(JsonElement array)
    {
        var entries = new List<LogEntry>();

        foreach (var item in array.EnumerateArray())
        {
            var line = item.TryGetProperty("line", out var lineEl) ? lineEl.GetString() ?? "" : "";
            var tsStr = item.TryGetProperty("timestamp", out var tsEl) ? tsEl.GetString() : null;

            var ts = DateTimeOffset.TryParse(tsStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;

            var level = "INFO";
            if (line.Contains("error", StringComparison.OrdinalIgnoreCase)) level = "ERROR";
            else if (line.Contains("warn", StringComparison.OrdinalIgnoreCase)) level = "WARN";

            entries.Add(new LogEntry(ts, level, line));
        }

        return entries.OrderByDescending(e => e.Timestamp).ToList();
    }

    private static double? ExtractPrometheusScalar(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Matrix: data.resultType=matrix, data.result[].values[][]
            if (root.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("result", out var results) && results.ValueKind == JsonValueKind.Array)
                {
                    var values = new List<double>();
                    foreach (var series in results.EnumerateArray())
                    {
                        if (!series.TryGetProperty("values", out var vals)) continue;
                        foreach (var point in vals.EnumerateArray())
                        {
                            if (point.GetArrayLength() < 2) continue;
                            var vStr = point[1].GetString();
                            if (double.TryParse(vStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                                values.Add(v);
                        }
                    }
                    return values.Count > 0 ? values.Average() : null;
                }

                // Vector instant
                if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var sample in data.EnumerateArray())
                    {
                        if (sample.TryGetProperty("value", out var val) && val.GetArrayLength() >= 2)
                        {
                            var vStr = val[1].GetString();
                            if (double.TryParse(vStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                                return v;
                        }
                    }
                }
            }
        }
        catch { /* fallback null */ }

        return null;
    }
}
