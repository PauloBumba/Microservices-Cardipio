using System.Diagnostics;
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
    IPromptTemplateStore       promptStore,
    ILogger<OllamaAiService>   logger) : IIncidentAiService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly ActivitySource _activitySource = new("AlertService.AI");

    public async Task<AiAnalysis> AnalyzeAsync(IncidentContext context, CancellationToken ct = default)
    {
        var instructions = await promptStore.GetTemplateAsync(ct);
        var prompt = BuildPrompt(context, instructions);

        using var activity = _activitySource.StartActivity("llm.generate", ActivityKind.Client);
        activity?.SetTag("gen_ai.system", "ollama");
        activity?.SetTag("gen_ai.operation.name", "chat");
        activity?.SetTag("gen_ai.request.model", options.Value.Model);
        activity?.SetTag("gen_ai.input.messages", JsonSerializer.Serialize(new[] { new { role = "user", content = prompt } }));

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

        try
        {
            var httpResponse = await http.PostAsync(
                $"{options.Value.BaseUrl}/api/generate",
                content,
                timeoutCts.Token);

            httpResponse.EnsureSuccessStatusCode();

            var raw = await httpResponse.Content.ReadFromJsonAsync<OllamaResponse>(_jsonOpts, timeoutCts.Token);
            if (raw?.Response is null)
            {
                activity?.SetTag("gen_ai.response.finish_reasons", "error");
                activity?.SetStatus(ActivityStatusCode.Error, "resposta vazia do Ollama");
                return FallbackAnalysis(context, options.Value.Model, reason: "resposta vazia do Ollama");
            }

            activity?.SetTag("gen_ai.response.model", raw.Model ?? options.Value.Model);
            activity?.SetTag("gen_ai.output.messages", JsonSerializer.Serialize(new[] { new { role = "assistant", content = raw.Response } }));
            activity?.SetTag("gen_ai.response.finish_reasons", "stop");
            activity?.SetTag("gen_ai.usage.prompt_tokens", raw.PromptEvalCount);
            activity?.SetTag("gen_ai.usage.completion_tokens", raw.EvalCount);
            activity?.SetTag("gen_ai.usage.total_tokens", raw.PromptEvalCount + raw.EvalCount);

            return ParseAiResponse(raw.Response, context, options.Value.Model);
        }
        catch (Exception ex)
        {
            activity?.SetTag("gen_ai.response.finish_reasons", "error");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Erro ao chamar Ollama");
            return FallbackAnalysis(context, options.Value.Model, reason: ex.Message);
        }
    }

    private static string BuildPrompt(IncidentContext ctx, string instructions)
    {
        var sb = new StringBuilder();

        sb.AppendLine(instructions.Trim());
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
    [JsonPropertyName("model")]             public string? Model           { get; set; }
    [JsonPropertyName("response")]          public string? Response        { get; set; }
    [JsonPropertyName("prompt_eval_count")] public int?    PromptEvalCount { get; set; }
    [JsonPropertyName("eval_count")]        public int?    EvalCount       { get; set; }
}
}