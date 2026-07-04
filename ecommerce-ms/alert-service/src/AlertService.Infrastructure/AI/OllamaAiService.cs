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
    public string Model      { get; set; } = "llama3.2:1b";
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
            Options = new OllamaModelOptions { Temperature = 0.1f }
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

    private static string BuildPrompt(IncidentContext ctx)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Você é um SRE especialista em aplicações distribuídas. Analise o incidente abaixo e responda APENAS em JSON válido.");
        sb.AppendLine("Formato obrigatório:");
        sb.AppendLine("""
        {
          "root_cause": "descrição técnica da causa raiz",
          "probable_cause": "deploy | database | overload | network | memory_leak | external_dependency | configuration | unknown",
          "severity": "Low | Medium | High | Critical",
          "operational_decision": "Ignore | Observe | Notify | Escalate",
          "confidence": 0.0,
          "impact": "impacto provável para usuário/sistema, máximo 1 frase",
          "evidence": ["evidência objetiva 1", "evidência objetiva 2"],
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
            sb.AppendLine($"## LOGS RELEVANTES ({Math.Min(ctx.Logs.Count, 12)} de {ctx.Logs.Count}):");
            foreach (var log in ctx.Logs.Take(12))
                sb.AppendLine($"- [{log.Timestamp:HH:mm:ss}] {log.Level}: {log.Message[..Math.Min(220, log.Message.Length)]}");
            sb.AppendLine();
        }

        if (ctx.Timeline.Count > 0)
        {
            sb.AppendLine("## TIMELINE:");
            foreach (var e in ctx.Timeline.TakeLast(10))
                sb.AppendLine($"- [{e.Timestamp:HH:mm:ss}] {e.Source}: {e.Description}");
            sb.AppendLine();
        }

        sb.AppendLine("Critério de decisão operacional:");
        sb.AppendLine("- Ignore: ruído, resolved sem impacto, no_data isolado ou evidência insuficiente.");
        sb.AppendLine("- Observe: warning sem evidência forte; registre mas não interrompa.");
        sb.AppendLine("- Notify: incidente real que merece mensagem em canal leve.");
        sb.AppendLine("- Escalate: critical, serviço down, perda de pedidos, outbox parado, fila crítica ou impacto claro ao usuário.");
        sb.AppendLine("Use confidence entre 0 e 1. Evidence deve conter apenas fatos vindos do alerta, logs ou métricas.");
        sb.AppendLine("Responda SOMENTE com o JSON, sem explicações adicionais.");

        return sb.ToString();
    }

    private AiAnalysis ParseAiResponse(string raw, IncidentContext ctx, string model)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end < 0)
                return FallbackAnalysis(ctx, model, $"JSON não encontrado na resposta: {raw[..Math.Min(100, raw.Length)]}");

            var json = raw[start..(end + 1)];
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var severityStr = GetString(root, "severity", "Medium");
            var severity = Enum.TryParse<AiSeverity>(severityStr, true, out var s) ? s : AiSeverity.Medium;

            var decisionStr = GetString(root, "operational_decision", "Notify");
            var decision = Enum.TryParse<AlertDecision>(decisionStr, true, out var d) ? d : AlertDecision.Notify;

            var confidence = root.TryGetProperty("confidence", out var confidenceEl) && confidenceEl.TryGetDouble(out var c)
                ? Math.Clamp(c, 0, 1)
                : 0.5;

            return new AiAnalysis
            {
                RootCause = GetString(root, "root_cause", "Desconhecido"),
                ProbableCause = GetString(root, "probable_cause", "unknown"),
                Severity = severity,
                OperationalDecision = decision,
                Confidence = confidence,
                Impact = GetString(root, "impact", "Impacto nao estimado"),
                Evidence = GetStringArray(root, "evidence"),
                HumanSummary = GetString(root, "human_summary", raw),
                Suggestions = GetStringArray(root, "suggestions"),
                GeneratedAt = DateTimeOffset.UtcNow,
                ModelUsed = model,
                IsReliable = true
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao parsear resposta do Ollama");
            return FallbackAnalysis(ctx, model, $"Parse error: {ex.Message}");
        }
    }

    private static string GetString(JsonElement root, string name, string fallback) =>
        root.TryGetProperty(name, out var el) ? el.GetString() ?? fallback : fallback;

    private static IReadOnlyList<string> GetStringArray(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList()
            : [];

    private static AiAnalysis FallbackAnalysis(IncidentContext ctx, string model, string reason) =>
        new()
        {
            RootCause = $"Análise AI indisponível ({reason})",
            ProbableCause = "unknown",
            Severity = AiSeverity.Medium,
            OperationalDecision = AlertDecision.Notify,
            Confidence = 0.2,
            Impact = "Impacto nao estimado porque a analise AI falhou",
            Evidence = [],
            HumanSummary = $"Incidente no serviço {ctx.Service}. Análise automática falhou — verificar manualmente.",
            Suggestions = ["Verificar logs no Grafana/Loki", "Checar dashboards do Prometheus"],
            GeneratedAt = DateTimeOffset.UtcNow,
            ModelUsed = model,
            IsReliable = false
        };

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