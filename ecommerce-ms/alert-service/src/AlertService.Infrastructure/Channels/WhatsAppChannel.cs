using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AlertService.Infrastructure.Channels;

public sealed class WhatsAppOptions
{
    /// <summary>evolution | zapi</summary>
    public string Provider { get; set; } = "evolution";

    // ── Evolution API ─────────────────────────────────────────────────────────
    public string EvolutionBaseUrl { get; set; } = "http://evolution-api:8080";
    public string EvolutionApiKey { get; set; } = "";
    public string EvolutionInstance { get; set; } = "cardipio";

    // ── Z-API ─────────────────────────────────────────────────────────────────
    public string ZApiInstanceId { get; set; } = "";
    public string ZApiToken { get; set; } = "";
    public string ZApiClientToken { get; set; } = "";

    // ── Destinatário ──────────────────────────────────────────────────────────
    /// <summary>Número ou lista separada por vírgula: 5511999999999,5511888888888</summary>
    public string Numbers { get; set; } = "";
}

public sealed class WhatsAppChannel(
    HttpClient http,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppChannel> logger) : IAlertChannel
{
    public string Name => "WhatsApp";

    public async Task SendAsync(AlertNotification notification, CancellationToken ct = default)
    {
        var opts = options.Value;
        var numbers = opts.Numbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (numbers.Length == 0)
        {
            logger.LogWarning("WhatsApp não configurado — nenhum número em Numbers");
            return;
        }

        var message = BuildMessage(notification);

        foreach (var number in numbers)
        {
            try
            {
                await (opts.Provider.ToLower() == "zapi"
                    ? SendZApiAsync(opts, number, message, ct)
                    : SendEvolutionAsync(opts, number, message, ct));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar WhatsApp para {Number}", number);
            }
        }
    }

    private async Task SendEvolutionAsync(WhatsAppOptions opts, string number, string message, CancellationToken ct)
    {
        var url = $"{opts.EvolutionBaseUrl}/message/sendText/{opts.EvolutionInstance}";
        var payload = new { number, text = message };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", opts.EvolutionApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    private async Task SendZApiAsync(WhatsAppOptions opts, string number, string message, CancellationToken ct)
    {
        var url = $"https://api.z-api.io/instances/{opts.ZApiInstanceId}/token/{opts.ZApiToken}/send-text";
        var payload = new { phone = number, message };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("Client-Token", opts.ZApiClientToken);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    private static string BuildMessage(AlertNotification n)
    {
        var emoji = n.State == "resolved" ? "✅" : n.Severity switch
        {
            AlertSeverity.Critical => "🔴",
            AlertSeverity.Warning => "🟡",
            _ => "🔵"
        };
        var status = n.State == "resolved" ? "RESOLVIDO" : "ALERTA";

        return $"""
            {emoji} *{status}: {n.Title}*

            {n.Body}

            🕐 {n.FiredAt:dd/MM/yyyy HH:mm:ss} UTC
            """;
    }
}
