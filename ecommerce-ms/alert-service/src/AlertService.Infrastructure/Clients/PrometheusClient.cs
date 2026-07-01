using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertService.Infrastructure.Clients;

public sealed class PrometheusOptions
{
    public string BaseUrl { get; set; } = "http://prometheus:9090";
}

public sealed class PrometheusClient(
    HttpClient                    http,
    IOptions<PrometheusOptions>   options,
    ILogger<PrometheusClient>     logger) : IPrometheusClient
{
    public async Task<IReadOnlyList<MetricSample>> QueryRangeAsync(
        string promQl,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeSpan step,
        CancellationToken ct = default)
    {
        var qs = HttpUtility.ParseQueryString(string.Empty);
        qs["query"] = promQl;
        qs["start"] = from.ToUnixTimeSeconds().ToString();
        qs["end"]   = to.ToUnixTimeSeconds().ToString();
        qs["step"]  = $"{(int)step.TotalSeconds}s";

        var url = $"{options.Value.BaseUrl}/api/v1/query_range?{qs}";
        logger.LogDebug("[Prometheus] {Query}", promQl);

        var response = await http.GetFromJsonAsync<PromResponse>(url, ct);
        if (response?.Data?.Result is null) return [];

        return ParseSamples(promQl, response.Data.Result);
    }

    public async Task<ServiceMetricsSnapshot> GetServiceSnapshotAsync(
        string service,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var step = TimeSpan.FromSeconds(30);

        var errorRateTask  = SafeScalarAsync(
            $$"""100 * sum(rate(http_requests_received_total{job="{{service}}",code=~"5.."}[5m])) / sum(rate(http_requests_received_total{job="{{service}}"}[5m]))""",
            from, to, step, ct);

        var latencyTask = SafeScalarAsync(
            $$"""histogram_quantile(0.99, sum by (le) (rate(http_request_duration_seconds_bucket{job="{{service}}"}[5m]))) * 1000""",
            from, to, step, ct);

        var cpuTask = SafeScalarAsync(
            $$"""rate(process_cpu_seconds_total{job="{{service}}"}[5m]) * 100""",
            from, to, step, ct);

        var memTask = SafeScalarAsync(
            $$"""process_working_set_bytes{job="{{service}}"} / 1024 / 1024""",
            from, to, step, ct);

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

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Retorna o valor médio da série ou null se não houver dados.</summary>
    private async Task<double?> SafeScalarAsync(
        string promQl,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeSpan step,
        CancellationToken ct)
    {
        try
        {
            var samples = await QueryRangeAsync(promQl, from, to, step, ct);
            if (samples.Count == 0) return null;
            return samples.Average(s => s.Value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Prometheus] Query falhou: {Query}", promQl);
            return null;
        }
    }

    private static IReadOnlyList<MetricSample> ParseSamples(string metricName, List<PromResult> results)
    {
        var samples = new List<MetricSample>();

        foreach (var result in results)
        {
            foreach (var value in result.Values)
            {
                if (value.Count < 2) continue;

                var ts    = DateTimeOffset.FromUnixTimeSeconds((long)value[0].GetDouble());
                var vStr  = value[1].GetString() ?? "0";
                var v     = double.TryParse(vStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;

                samples.Add(new MetricSample(ts, metricName, v, result.Metric));
            }
        }

        return samples.AsReadOnly();
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class PromResponse
    {
        [JsonPropertyName("data")] public PromData? Data { get; set; }
    }

    private sealed class PromData
    {
        [JsonPropertyName("result")] public List<PromResult>? Result { get; set; }
    }

    private sealed class PromResult
    {
        [JsonPropertyName("metric")] public Dictionary<string, string> Metric { get; set; } = [];
        [JsonPropertyName("values")] public List<List<System.Text.Json.JsonElement>> Values { get; set; } = [];
    }
}
