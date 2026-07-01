using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using AlertService.Core.Services;
using Microsoft.Extensions.Logging;

namespace AlertService.Core;

/// <summary>
/// MODIFICAÇÃO ADITIVA: mantém o comportamento legacy 100% preservado.
/// Quando AiEnabled=false: exatamente o mesmo fluxo de antes.
/// Quando AiEnabled=true: enriquece → notifica com contexto.
/// </summary>
public sealed class AlertDispatcher(
    IEnumerable<IAlertChannel> channels,
    IIncidentEnricher enricher,
    FeatureFlags flags,
    ILogger<AlertDispatcher> logger)
{
    public async Task DispatchAsync(GrafanaWebhookPayload payload, CancellationToken ct = default)
    {
        AlertNotification notification;

        if (flags.AiEnabled || flags.LokiEnrichmentEnabled || flags.PrometheusEnrichmentEnabled || flags.McpEnrichmentEnabled)
        {
            notification = await DispatchEnhancedAsync(payload, ct);
        }
        else
        {
            // ── MODO LEGACY: exatamente o mesmo código de antes ───────────────
            notification = BuildNotification(payload);
        }

        logger.LogInformation(
            "Disparando alerta [{State}] '{Title}' para {Count} canais",
            notification.State, notification.Title, channels.Count());

        var tasks = channels.Select(ch => SendSafeAsync(ch, notification, ct));
        await Task.WhenAll(tasks);
    }

    // ── MODO ENHANCED ─────────────────────────────────────────────────────────

    private async Task<AlertNotification> DispatchEnhancedAsync(
        GrafanaWebhookPayload payload,
        CancellationToken ct)
    {
        IncidentContext? context = null;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(flags.EnrichmentTimeout);

            context = await enricher.EnrichAsync(payload, timeoutCts.Token);
        }
        catch (Exception ex)
        {
            // Falha no enriquecimento NÃO bloqueia a notificação
            logger.LogError(ex, "Pipeline de enriquecimento falhou — caindo para modo legacy");
        }

        if (context is null)
            return BuildNotification(payload);

        return BuildEnrichedNotification(payload, context);
    }

    // ── Builders ──────────────────────────────────────────────────────────────

    /// <summary>
    /// IDÊNTICO ao BuildNotification original — não toque.
    /// </summary>
    private static AlertNotification BuildNotification(GrafanaWebhookPayload payload)
    {
        var alerts = payload.AlertsList;
        var isFiring = alerts.Any(a => a.Status == "firing");
        var state = isFiring ? "firing" : "resolved";

        var first = alerts.FirstOrDefault();
        var labels = first?.Labels ?? new Dictionary<string, string>();
        var annotations = first?.Annotations ?? new Dictionary<string, string>();

        var alertName = labels.GetValueOrDefault("rulename")
            ?? labels.GetValueOrDefault("alertname")
            ?? payload.Title;
        var service = labels.GetValueOrDefault("service")
            ?? labels.GetValueOrDefault("job");
        var folder = labels.GetValueOrDefault("grafana_folder");
        var severityLabel = labels.GetValueOrDefault("severity", "info");
        var severity = severityLabel.ToLowerInvariant() switch
        {
            "critical" => AlertSeverity.Critical,
            "warning" => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };

        var statusEmoji = state == "resolved" ? "✅" : "🔥";
        var severityEmoji = severity switch
        {
            AlertSeverity.Critical => "🔴",
            AlertSeverity.Warning => "🟡",
            _ => "🔵"
        };
        var statusLabel = state == "resolved" ? "RESOLVIDO" : "DISPARADO";

        var titleParts = new List<string> { alertName };
        if (!string.IsNullOrWhiteSpace(service)) titleParts.Add($"({service})");
        var title = string.Join(" ", titleParts);

        var bodyLines = new List<string>
        {
            $"{statusEmoji} {statusLabel} {severityEmoji} {severityLabel.ToUpperInvariant()}"
        };

        var summary = annotations.GetValueOrDefault("summary");
        if (!string.IsNullOrWhiteSpace(summary) && !summary.Contains("[no value]"))
            bodyLines.Add($"Resumo: {summary}");

        var description = annotations.GetValueOrDefault("description");
        if (!string.IsNullOrWhiteSpace(description) && description != summary)
            bodyLines.Add($"Descrição: {description}");

        if (!string.IsNullOrWhiteSpace(service))
            bodyLines.Add($"Serviço: {service}");
        if (!string.IsNullOrWhiteSpace(folder))
            bodyLines.Add($"Pasta Grafana: {folder}");

        var ruleId = labels.GetValueOrDefault("ref_id");
        if (!string.IsNullOrWhiteSpace(ruleId))
            bodyLines.Add($"Query ref: {ruleId}");

        if (first?.StartsAt is { } startsAt)
            bodyLines.Add($"Desde: {startsAt:dd/MM/yyyy HH:mm:ss} UTC");

        if (!string.IsNullOrWhiteSpace(first?.GeneratorURL))
            bodyLines.Add($"Painel: {first.GeneratorURL}");

        if (alerts.Count > 1)
        {
            bodyLines.Add($"--- {alerts.Count} alertas neste grupo ---");
            foreach (var a in alerts)
            {
                var aName = a.Labels?.GetValueOrDefault("rulename")
                    ?? a.Labels?.GetValueOrDefault("alertname")
                    ?? "alerta";
                var aSeverity = a.Labels?.GetValueOrDefault("severity") ?? "info";
                bodyLines.Add($"• [{a.Status.ToUpperInvariant()}] {aName} ({aSeverity})");
            }
        }

        var body = string.Join("\n", bodyLines);

        return new AlertNotification(
            Title: title,
            Body: body,
            Severity: severity,
            State: state,
            FiredAt: first?.StartsAt ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Versão enriquecida: inclui RCA, timeline e anomalias no body.
    /// </summary>
    private static AlertNotification BuildEnrichedNotification(
        GrafanaWebhookPayload payload,
        IncidentContext context)
    {
        // Reutiliza o builder legacy para o bloco base
        var legacy = BuildNotification(payload);

        var bodyLines = new List<string> { legacy.Body };

        if (context.EnrichmentSource is "mcp")
            bodyLines.Add("\n🔗 Enriquecido via MCP (Grafana/Loki/Prometheus)");

        // ── Seção AI ─────────────────────────────────────────────────────────
        if (context.AiAnalysis is { } ai)
        {
            bodyLines.Add("\n🧠 *ANÁLISE DE ROOT CAUSE (AI)*");
            bodyLines.Add($"Causa raiz: {ai.RootCause}");
            bodyLines.Add($"Provável origem: {ai.ProbableCause}");
            bodyLines.Add($"Severidade AI: {ai.Severity}");
            bodyLines.Add($"Resumo: {ai.HumanSummary}");

            if (ai.Suggestions.Count > 0)
            {
                bodyLines.Add("Ações sugeridas:");
                foreach (var s in ai.Suggestions)
                    bodyLines.Add($"  • {s}");
            }
        }

        // ── Anomalias ─────────────────────────────────────────────────────────
        if (context.Anomalies.Count > 0)
        {
            bodyLines.Add("\n📊 *ANOMALIAS DETECTADAS*");
            foreach (var anomaly in context.Anomalies)
                bodyLines.Add($"  ⚠ {anomaly.Description}");
        }

        // ── Timeline (top 5 eventos) ──────────────────────────────────────────
        if (context.Timeline.Count > 0)
        {
            bodyLines.Add("\n📅 *TIMELINE*");
            foreach (var evt in context.Timeline.TakeLast(5))
                bodyLines.Add($"  [{evt.Timestamp:HH:mm:ss}] {evt.Source.ToUpperInvariant()}: {evt.Description}");
        }

        // ── Logs de erro (top 3) ──────────────────────────────────────────────
        var errorLogs = context.Logs
            .Where(l => l.Level is "ERROR")
            .TakeLast(3)
            .ToList();

        if (errorLogs.Count > 0)
        {
            bodyLines.Add($"\n📋 *ÚLTIMOS ERRORS NO LOKI* ({context.Logs.Count} logs no período)");
            foreach (var log in errorLogs)
                bodyLines.Add($"  [{log.Timestamp:HH:mm:ss}] {log.Message[..Math.Min(100, log.Message.Length)]}");
        }

        bodyLines.Add($"\n🆔 Incident ID: {context.IncidentId}");

        return new AlertNotification(
            Title: legacy.Title,
            Body: string.Join("\n", bodyLines),
            Severity: legacy.Severity,
            State: legacy.State,
            FiredAt: legacy.FiredAt,
            IncidentContext: context);
    }

    private async Task SendSafeAsync(
        IAlertChannel channel,
        AlertNotification notification,
        CancellationToken ct)
    {
        try
        {
            await channel.SendAsync(notification, ct);
            logger.LogInformation("Alerta enviado via {Channel}", channel.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar alerta via {Channel}", channel.Name);
        }
    }
}
