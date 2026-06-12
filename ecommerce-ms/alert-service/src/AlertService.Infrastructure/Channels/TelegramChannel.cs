using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AlertService.Infrastructure.Channels;

public sealed class TelegramOptions
{
    public string BotToken { get; set; } = "";
    public string ChatId { get; set; } = "";  // pode ser ID numérico ou @grupo
}

public sealed class TelegramChannel(
    HttpClient http,
    IOptions<TelegramOptions> options,
    ILogger<TelegramChannel> logger) : IAlertChannel
{
    public string Name => "Telegram";

    public async Task SendAsync(AlertNotification notification, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.BotToken) || string.IsNullOrWhiteSpace(opts.ChatId))
        {
            logger.LogWarning("Telegram não configurado — BotToken ou ChatId ausente");
            return;
        }

        var emoji = notification.State == "resolved" ? "✅" : notification.Severity switch
        {
            AlertSeverity.Critical => "🔴",
            AlertSeverity.Warning => "🟡",
            _ => "🔵"
        };

        var statusLabel = notification.State == "resolved" ? "RESOLVIDO" : "ALERTA";

        var text = $"""
            {emoji} *{statusLabel}: {EscapeMd(notification.Title)}*
            
            {EscapeMd(notification.Body)}
            
            🕐 {notification.FiredAt:dd/MM/yyyy HH:mm:ss} UTC
            """;

        var payload = new
        {
            chat_id = opts.ChatId,
            text,
            parse_mode = "MarkdownV2"
        };

        var url = $"https://api.telegram.org/bot{opts.BotToken}/sendMessage";
        var json = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await http.PostAsync(url, json, ct);
        response.EnsureSuccessStatusCode();
    }

    // Telegram MarkdownV2 precisa escapar caracteres especiais
    private static string EscapeMd(string text) =>
        text.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[")
            .Replace("]", "\\]").Replace("(", "\\(").Replace(")", "\\)")
            .Replace("~", "\\~").Replace("`", "\\`").Replace(">", "\\>")
            .Replace("#", "\\#").Replace("+", "\\+").Replace("-", "\\-")
            .Replace("=", "\\=").Replace("|", "\\|").Replace("{", "\\{")
            .Replace("}", "\\}").Replace(".", "\\.").Replace("!", "\\!");
}
