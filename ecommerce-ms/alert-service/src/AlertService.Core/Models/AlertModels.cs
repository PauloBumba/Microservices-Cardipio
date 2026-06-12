namespace AlertService.Core.Models;

// ─── Payload enviado pelo Grafana Alerting via Contact Point (Webhook) ────────
public sealed record GrafanaWebhookPayload(
    string Title,
    string Message,
    string State,           // "alerting" | "ok" | "no_data"
    string OrgName,
    List<GrafanaAlert> Alerts
);

public sealed record GrafanaAlert(
    string Status,          // "firing" | "resolved"
    Dictionary<string, string> Labels,
    Dictionary<string, string> Annotations,
    string? GeneratorURL,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt
);

// ─── Modelo interno normalizado ───────────────────────────────────────────────
public sealed record AlertNotification(
    string Title,
    string Body,
    AlertSeverity Severity,
    string State,           // "firing" | "resolved"
    DateTimeOffset FiredAt
);

public enum AlertSeverity { Critical, Warning, Info }
