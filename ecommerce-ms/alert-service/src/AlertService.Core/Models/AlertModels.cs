namespace AlertService.Core.Models;

// ─── Payload enviado pelo Grafana Alerting via Contact Point (Webhook) ───────
// Formato real do Grafana (Unified Alerting), campos extras são ignorados
// automaticamente pelo System.Text.Json. Os campos abaixo cobrem o que
// realmente vem no JSON: https://grafana.com/docs/grafana/latest/alerting/manage-notifications/webhook-notifier/
public sealed record GrafanaWebhookPayload(
    string Title,
    string Message,
    string State,                 // "alerting" | "ok" | "no_data"
    int? OrgId = null,             // Grafana manda orgId (int), não orgName
    List<GrafanaAlert>? Alerts = null
)
{
    // Mantém compatibilidade com código existente que usa OrgName
    public string OrgName => OrgId?.ToString() ?? "N/A";

    // Garante que nunca é null ao iterar
    public List<GrafanaAlert> AlertsList => Alerts ?? [];
}

public sealed record GrafanaAlert(
    string Status,          // "firing" | "resolved"
    Dictionary<string, string>? Labels = null,
    Dictionary<string, string>? Annotations = null,
    string? GeneratorURL = null,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null
);

// ─── Modelo interno normalizado ───────────────────────────────────────────
public sealed record AlertNotification(
    string Title,
    string Body,
    AlertSeverity Severity,
    string State,           // "firing" | "resolved"
    DateTimeOffset FiredAt
);

public enum AlertSeverity { Critical, Warning, Info }