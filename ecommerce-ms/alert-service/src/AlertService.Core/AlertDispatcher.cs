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
            // Não propaga — um canal com falha não bloqueia os outros
        }
    }
    private static AlertNotification BuildNotification(GrafanaWebhookPayload payload)
    {
        var alerts = payload.AlertsList;
        var isFiring = alerts.Any(a => a.Status == "firing");
        var state = isFiring ? "firing" : "resolved";
        var severity = payload.Title.ToLowerInvariant() switch
        {
            var t when t.Contains("down") || t.Contains("critical") => AlertSeverity.Critical,
            var t when t.Contains("memory") || t.Contains("queue") || t.Contains("error") => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };
        // Formata o body com detalhes de cada alerta
        var details = alerts
            .Select(a =>
            {
                var labels = string.Join(", ", (a.Labels ?? new Dictionary<string, string>())
                    .Select(kv => $"{kv.Key}={kv.Value}"));
                return $"- [{a.Status.ToUpper()}] {labels}";
            });
        var body = string.IsNullOrWhiteSpace(payload.Message)
            ? string.Join("\n", details)
            : $"{payload.Message}\n\n{string.Join("\n", details)}";
        return new AlertNotification(
            Title: payload.Title,
            Body: body,
            Severity: severity,
            State: state,
            FiredAt: alerts.FirstOrDefault()?.StartsAt ?? DateTimeOffset.UtcNow
        );
    }
}