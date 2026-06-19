using AlertService.Core;
using AlertService.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlertService.API.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class WebhookController(
    AlertDispatcher dispatcher,
    ILogger<WebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Endpoint configurado no Grafana como Contact Point (Webhook).
    /// URL: http://alert-service:8080/api/alerts/webhook
    /// Autenticação: Authorization: Bearer {Webhook:Secret}
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromBody] GrafanaWebhookPayload payload,
        CancellationToken ct)
    {
        var expectedToken = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Webhook:Secret"];

        if (!string.IsNullOrWhiteSpace(expectedToken))
        {
            var authHeader = Request.Headers.Authorization.ToString();
            // Aceita "Bearer <token>" ou o token puro, e também o header legado X-Alert-Token
            var receivedToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : (authHeader.Trim() is { Length: > 0 } a ? a : Request.Headers["X-Alert-Token"].ToString());

            logger.LogInformation("Webhook auth header recebido: '{Auth}' (esperado token configurado)", authHeader);

            if (receivedToken != expectedToken)
            {
                logger.LogWarning("Webhook recebido com token inválido. Header Authorization recebido: '{Auth}'", authHeader);
                return Unauthorized();
            }
        }

        logger.LogInformation(
            "Webhook recebido: [{State}] {Title} - {Count} alerts",
            payload.State, payload.Title, payload.AlertsList.Count);

        await dispatcher.DispatchAsync(payload, ct);
        return Ok(new { received = true, channels = 3 });
    }

    /// <summary>Health check simples pra testar se o serviço tá up</summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow });

    /// <summary>
    /// Endpoint de teste — dispara um alerta fake pra validar os canais.
    /// POST /api/alerts/test?severity=critical
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> Test([FromQuery] string severity = "warning", CancellationToken ct = default)
    {
        var payload = new GrafanaWebhookPayload(
            Title: $"[TESTE] Alerta de {severity}",
            Message: "Este é um alerta de teste disparado manualmente.",
            State: "alerting",
            OrgId: 1,
            Alerts:
            [
                new GrafanaAlert(
                    Status: "firing",
                    Labels: new Dictionary<string, string>
                    {
                        ["alertname"] = "TestAlert",
                        ["severity"] = severity,
                        ["service"] = "alert-service"
                    },
                    Annotations: new Dictionary<string, string>
                    {
                        ["summary"] = "Alerta de teste disparado via API"
                    },
                    GeneratorURL: null,
                    StartsAt: DateTimeOffset.UtcNow,
                    EndsAt: null
                )
            ]
        );

        await dispatcher.DispatchAsync(payload, ct);
        return Ok(new { sent = true, severity });
    }
}