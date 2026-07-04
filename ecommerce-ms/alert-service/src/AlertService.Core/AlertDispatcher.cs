using System.Collections.Concurrent;
using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using AlertService.Core.Services;
using Microsoft.Extensions.Logging;

namespace AlertService.Core;

/// <summary>
/// Mantem o modo legacy quando enriquecimento esta desligado.
/// Quando MCP/AI estao ligados, enriquece, classifica e aplica politica anti-ruido.
/// </summary>
public sealed class AlertDispatcher(
    IEnumerable<IAlertChannel> channels,
    IIncidentEnricher enricher,
    FeatureFlags flags,
    ILogger<AlertDispatcher> logger)
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _lastSentByIncidentKey = new();
    private static readonly TimeSpan DuplicateCooldown = TimeSpan.FromMinutes(15);

    public async Task DispatchAsync(GrafanaWebhookPayload payload, CancellationToken ct = default)
    {
        AlertNotification notification;

        if (flags.AiEnabled || flags.LokiEnrichmentEnabled || flags.PrometheusEnrichmentEnabled || flags.McpEnrichmentEnabled)
            notification = await DispatchEnhancedAsync(payload, ct);
        else
            notification = BuildNotification(payload);

        var selectedChannels = SelectChannels(notification).ToList();
        if (selectedChannels.Count == 0)
        {
            logger.LogInformation(
                "Alerta [{State}] '{Title}' registrado sem notificacao externa pela politica operacional",
                notification.State, notification.Title);
            return;
        }

        if (ShouldSuppressDuplicate(notification))
        {
            logger.LogInformation(
                "Alerta duplicado suprimido por {Cooldown}min: {Title}",
                DuplicateCooldown.TotalMinutes, notification.Title);
            return;
        }

        logger.LogInformation(
            "Disparando alerta [{State}] '{Title}' para {Count} canais",
            notification.State, notification.Title, selectedChannels.Count);

        var tasks = selectedChannels.Select(ch => SendSafeAsync(ch, notification, ct));
        await Task.WhenAll(tasks);
    }

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
            logger.LogError(ex, "Pipeline de enriquecimento falhou — caindo para modo legacy");
        }

        return context is null ? BuildNotification(payload) : BuildEnrichedNotification(payload, context);
    }

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

        return new AlertNotification(
            Title: title,
            Body: string.Join("\n", bodyLines),
            Severity: severity,
            State: state,
            FiredAt: first?.StartsAt ?? DateTimeOffset.UtcNow);
    }

    private static AlertNotification BuildEnrichedNotification(
        GrafanaWebhookPayload payload,
        IncidentContext context)
    {
        var legacy = BuildNotification(payload);
        var bodyLines = new List<string> { legacy.Body };

        if (context.EnrichmentSource is "mcp")
            bodyLines.Add("\n🔗 Enriquecido via MCP (Grafana/Loki/Prometheus)");

        if (context.AiAnalysis is { } ai)
        {
            bodyLines.Add("\n🧠 *ANÁLISE DE INCIDENTE (AI)*");
            bodyLines.Add($"Decisão operacional: {ai.OperationalDecision} (confiança {ai.Confidence:P0})");
            bodyLines.Add($"Causa raiz: {ai.RootCause}");
            bodyLines.Add($"Provável origem: {ai.ProbableCause}");
            bodyLines.Add($"Severidade AI: {ai.Severity}");
            bodyLines.Add($"Impacto: {ai.Impact}");
            bodyLines.Add($"Resumo: {ai.HumanSummary}");

            if (ai.Evidence.Count > 0)
            {
                bodyLines.Add("Evidências:");
                foreach (var evidence in ai.Evidence.Take(4))
                    bodyLines.Add($"  • {evidence}");
            }

            if (ai.Suggestions.Count > 0)
            {
                bodyLines.Add("Ações sugeridas:");
                foreach (var suggestion in ai.Suggestions.Take(5))
                    bodyLines.Add($"  • {suggestion}");
            }
        }

        if (context.Anomalies.Count > 0)
        {
            bodyLines.Add("\n📊 *ANOMALIAS DETECTADAS*");
            foreach (var anomaly in context.Anomalies)
                bodyLines.Add($"  ⚠ {anomaly.Description}");
        }

        if (context.Timeline.Count > 0)
        {
            bodyLines.Add("\n📅 *TIMELINE*");
            foreach (var evt in context.Timeline.TakeLast(5))
                bodyLines.Add($"  [{evt.Timestamp:HH:mm:ss}] {evt.Source.ToUpperInvariant()}: {evt.Description}");
        }

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

    private IEnumerable<IAlertChannel> SelectChannels(AlertNotification notification)
    {
        var decision = notification.IncidentContext?.AiAnalysis?.OperationalDecision
            ?? (notification.Severity is AlertSeverity.Critical ? AlertDecision.Escalate : AlertDecision.Notify);

        if (decision is AlertDecision.Ignore or AlertDecision.Observe)
            return [];

        if (notification.State == "resolved" && notification.Severity is not AlertSeverity.Critical)
            return [];

        return decision is AlertDecision.Escalate || notification.Severity is AlertSeverity.Critical
            ? channels
            : channels.Where(ch => !string.Equals(ch.Name, "Email", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldSuppressDuplicate(AlertNotification notification)
    {
        if (notification.State == "resolved") return false;

        var service = notification.IncidentContext?.Service ?? "unknown";
        var decision = notification.IncidentContext?.AiAnalysis?.OperationalDecision.ToString() ?? "legacy";
        var key = $"{service}|{notification.Title}|{notification.Severity}|{decision}".ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        if (_lastSentByIncidentKey.TryGetValue(key, out var last) && now - last < DuplicateCooldown)
            return true;

        _lastSentByIncidentKey[key] = now;
        return false;
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