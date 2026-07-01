using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertService.Infrastructure.AI;

public sealed class OllamaOptions
{
    public string BaseUrl    { get; set; } = "http://localhost:11434";
    public string Model      { get; set; } = "llama3.2:1b";  // 1b é rápido; troque por mistral se tiver GPU
    public int    TimeoutSec { get; set; } = 30;
}

public sealed class OllamaAiService(
    HttpClient                 http,
    IOptions<OllamaOptions>    options,
    ILogger<OllamaAiService>   logger) : IIncidentAiService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiAnalysis> AnalyzeAsync(IncidentContext context, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(context);

        var request = new OllamaRequest
        {
            Model  = options.Value.Model,
            Prompt = prompt,
            Stream = false,
            Options = new OllamaModelOptions { Temperature = 0.1f } // baixa temp = mais determinístico
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSec));

        var httpResponse = await http.PostAsync(
            $"{options.Value.BaseUrl}/api/generate",
            content,
            timeoutCts.Token);

        httpResponse.EnsureSuccessStatusCode();

        var raw = await httpResponse.Content.ReadFromJsonAsync<OllamaResponse>(_jsonOpts, timeoutCts.Token);
        if (raw?.Response is null)
            return FallbackAnalysis(context, options.Value.Model, reason: "resposta vazia do Ollama");

        return ParseAiResponse(raw.Response, context, options.Value.Model);
    }

    // ── Prompt engineering ────────────────────────────────────────────────────

    private static string BuildPrompt(IncidentContext ctx)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Você é um SRE especialista. Analise o incidente abaixo e responda APENAS em JSON válido.");
        sb.AppendLine("Formato obrigatório:");
        sb.AppendLine("""
        {
          "root_cause": "descrição técnica da causa raiz",
          "probable_cause": "uma das opções: deploy | database | overload | network | memory_leak | external_dependency | configuration | unknown",
          "severity": "Low | Medium | High | Critical",
          "human_summary": "resumo em português para o time, máximo 2 frases",
          "suggestions": ["ação 1", "ação 2", "ação 3"]
        }
        """);
        sb.AppendLine();

        sb.AppendLine($"# INCIDENTE: {ctx.OriginalPayload.Title}");
        sb.AppendLine($"Serviço: {ctx.Service}");
        sb.AppendLine($"Início: {ctx.AlertFiredAt:dd/MM/yyyy HH:mm:ss} UTC");
        sb.AppendLine($"Estado: {ctx.OriginalPayload.State}");
        sb.AppendLine();

        if (ctx.Metrics is not null)
        {
            sb.AppendLine("## MÉTRICAS NO PERÍODO:");
            if (ctx.Metrics.ErrorRatePercent.HasValue)
                sb.AppendLine($"- Error rate: {ctx.Metrics.ErrorRatePercent:F2}%");
            if (ctx.Metrics.LatencyP99Ms.HasValue)
                sb.AppendLine($"- Latência p99: {ctx.Metrics.LatencyP99Ms:F0}ms");
            if (ctx.Metrics.CpuUsagePercent.HasValue)
                sb.AppendLine($"- CPU: {ctx.Metrics.CpuUsagePercent:F1}%");
            if (ctx.Metrics.MemoryUsageMb.HasValue)
                sb.AppendLine($"- Memória: {ctx.Metrics.MemoryUsageMb:F0}MB");
            sb.AppendLine();
        }

        if (ctx.Anomalies.Count > 0)
        {
            sb.AppendLine("## ANOMALIAS DETECTADAS (z-score > 2σ):");
            foreach (var a in ctx.Anomalies)
                sb.AppendLine($"- {a.Description}");
            sb.AppendLine();
        }

        if (ctx.Logs.Count > 0)
        {
            sb.AppendLine($"## LOGS DE ERRO ({Math.Min(ctx.Logs.Count, 10)} de {ctx.Logs.Count}):");
            foreach (var log in ctx.Logs.Where(l => l.Level is "ERROR").Take(10))
                sb.AppendLine($"- [{log.Timestamp:HH:mm:ss}] {log.Message[..Math.Min(200, log.Message.Length)]}");
            sb.AppendLine();
        }

        if (ctx.Timeline.Count > 0)
        {
            sb.AppendLine("## TIMELINE:");
            foreach (var e in ctx.Timeline.TakeLast(10))
                sb.AppendLine($"- [{e.Timestamp:HH:mm:ss}] {e.Source}: {e.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("Responda SOMENTE com o JSON, sem explicações adicionais.");

        return sb.ToString();
    }

    // ── Parser ────────────────────────────────────────────────────────────────

    private AiAnalysis ParseAiResponse(string raw, IncidentContext ctx, string model)
    {
        try
        {
            // Extrai JSON mesmo se vier com texto ao redor
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end < 0)
                return FallbackAnalysis(ctx, model, $"JSON não encontrado na resposta: {raw[..Math.Min(100, raw.Length)]}");

            var json    = raw[start..(end + 1)];
            var doc     = JsonDocument.Parse(json);
            var root    = doc.RootElement;

            var severityStr = root.GetProperty("severity").GetString() ?? "Medium";
            var severity    = Enum.TryParse<AiSeverity>(severityStr, true, out var s) ? s : AiSeverity.Medium;

            var suggestions = root.TryGetProperty("suggestions", out var sugsEl)
                ? sugsEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                : [];

            return new AiAnalysis
            {
                RootCause     = root.GetProperty("root_cause").GetString()     ?? "Desconhecido",
                ProbableCause = root.GetProperty("probable_cause").GetString() ?? "unknown",
                Severity      = severity,
                HumanSummary  = root.GetProperty("human_summary").GetString()  ?? raw,
                Suggestions   = suggestions,
                GeneratedAt   = DateTimeOffset.UtcNow,
                ModelUsed     = model,
                IsReliable    = true
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao parsear resposta do Ollama");
            return FallbackAnalysis(ctx, model, $"Parse error: {ex.Message}");
        }
    }

    private static AiAnalysis FallbackAnalysis(IncidentContext ctx, string model, string reason) =>
        new()
        {
            RootCause     = $"Análise AI indisponível ({reason})",
            ProbableCause = "unknown",
            Severity      = AiSeverity.Medium,
            HumanSummary  = $"Incidente no serviço {ctx.Service}. Análise automática falhou — verificar manualmente.",
            Suggestions   = ["Verificar logs no Grafana/Loki", "Checar dashboards do Prometheus"],
            GeneratedAt   = DateTimeOffset.UtcNow,
            ModelUsed     = model,
            IsReliable    = false
        };

    // ── Ollama DTOs ───────────────────────────────────────────────────────────

    private sealed class OllamaRequest
    {
        [JsonPropertyName("model")]   public string  Model  { get; set; } = "";
        [JsonPropertyName("prompt")]  public string  Prompt { get; set; } = "";
        [JsonPropertyName("stream")]  public bool    Stream { get; set; }
        [JsonPropertyName("options")] public OllamaModelOptions? Options { get; set; }
    }

    private sealed class OllamaModelOptions
    {
        [JsonPropertyName("temperature")] public float Temperature { get; set; }
    }

    private sealed class OllamaResponse
    {
        [JsonPropertyName("response")] public string? Response { get; set; }
    }
}
