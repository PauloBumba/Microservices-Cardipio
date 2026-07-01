using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertService.Infrastructure.Clients;

public sealed class LokiOptions
{
    public string BaseUrl { get; set; } = "http://loki:3100";
    public int    QueryLimitLines { get; set; } = 100;
}

public sealed class LokiClient(
    HttpClient                 http,
    IOptions<LokiOptions>      options,
    ILogger<LokiClient>        logger) : ILokiClient
{
    // Loki Query Range API: GET /loki/api/v1/query_range
    // Docs: https://grafana.com/docs/loki/latest/reference/loki-http-api/#query-logs-within-a-range-of-time

    public async Task<IReadOnlyList<LogEntry>> QueryRangeAsync(
        string         service,
        DateTimeOffset from,
        DateTimeOffset to,
        int            limit = 100,
        CancellationToken ct = default)
    {
        // LogQL: filtra pelo label {service=...} e pega ERROR/WARN no período
        var logql = $$"""{service="{{service}}"} | json | level=~"(?i)error|warn|warning" """;

        var qs = HttpUtility.ParseQueryString(string.Empty);
        qs["query"] = logql;
        qs["start"] = ToNanoseconds(from).ToString();
        qs["end"]   = ToNanoseconds(to).ToString();
        qs["limit"] = limit.ToString();
        qs["direction"] = "backward";

        var url = $"{options.Value.BaseUrl}/loki/api/v1/query_range?{qs}";

        logger.LogDebug("[Loki] Query: {Url}", url);

        var response = await http.GetFromJsonAsync<LokiQueryResponse>(url, ct);
        if (response?.Data?.Result is null || response.Data.Result.Count == 0)
            return [];

        return ParseEntries(response.Data.Result);
    }

    // ── Parsers ───────────────────────────────────────────────────────────────

    private static IReadOnlyList<LogEntry> ParseEntries(List<LokiStream> streams)
    {
        var entries = new List<LogEntry>();

        foreach (var stream in streams)
        {
            var level = stream.Stream.GetValueOrDefault("level")
                     ?? stream.Stream.GetValueOrDefault("severity")
                     ?? "INFO";

            foreach (var value in stream.Values)
            {
                // value[0] = timestamp nanosegundos (string), value[1] = log line
                if (value.Count < 2) continue;

                var nsStr  = value[0].GetString()!;
                var line   = value[1].GetString() ?? "";
                var ts     = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(nsStr) / 1_000_000);

                // Tenta extrair traceId do JSON de log estruturado
                string? traceId = null;
                if (line.TrimStart().StartsWith('{'))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(line);
                        doc.RootElement.TryGetProperty("TraceId", out var t);
                        traceId = t.ValueKind == JsonValueKind.Undefined ? null : t.GetString();
                    }
                    catch { /* log não é JSON */ }
                }

                entries.Add(new LogEntry(
                    Timestamp: ts,
                    Level:     level.ToUpperInvariant(),
                    Message:   line,
                    TraceId:   traceId));
            }
        }

        return entries
            .OrderByDescending(e => e.Timestamp)
            .ToList()
            .AsReadOnly();
    }

    private static long ToNanoseconds(DateTimeOffset dt) =>
        dt.ToUnixTimeMilliseconds() * 1_000_000L;

    // ── DTOs internos ─────────────────────────────────────────────────────────

    private sealed class LokiQueryResponse
    {
        [JsonPropertyName("data")] public LokiData? Data { get; set; }
    }

    private sealed class LokiData
    {
        [JsonPropertyName("result")] public List<LokiStream>? Result { get; set; }
    }

    private sealed class LokiStream
    {
        [JsonPropertyName("stream")] public Dictionary<string, string> Stream { get; set; } = [];
        [JsonPropertyName("values")] public List<List<JsonElement>>    Values { get; set; } = [];
    }
}
