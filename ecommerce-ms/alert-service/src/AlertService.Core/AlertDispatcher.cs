using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using Microsoft.Extensions.Logging;
namespace AlertService.Core;

public sealed class AlertDispatcher(
    IEnumerable<IAlertChannel> channels,
    ILogger<AlertDispatcher> logger)
{
    public async Task DispatchAsync(GrafanaWebhookPayload payload, CancellationToken ct = default)
    {
        var notification = BuildNotification(payload);
        logger.LogInformation(
            "Disparando alerta [{State}] '{Title}' para {Count} canais",
            notification.State, notification.Title, channels.Count());
        var tasks = channels.Select(ch => SendSafeAsync(ch, notification, ct));
        await Task.WhenAll(tasks);
    }

    private async Task SendSafeAsync(IAlertChannel channel, AlertNotification notification, CancellationToken ct)
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
        var service = labels.GetValueOrDefault("service");
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
            FiredAt: first?.StartsAt ?? DateTimeOffset.UtcNow
        );
    }
}