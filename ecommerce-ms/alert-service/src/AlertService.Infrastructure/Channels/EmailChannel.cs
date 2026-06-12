using AlertService.Core.Interfaces;
using AlertService.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AlertService.Infrastructure.Channels;

public sealed class EmailOptions
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromName { get; set; } = "ECommerce Alertas";
    public string FromAddress { get; set; } = "";
    /// <summary>Destinatários separados por vírgula</summary>
    public string ToAddresses { get; set; } = "";
}

public sealed class EmailChannel(
    IOptions<EmailOptions> options,
    ILogger<EmailChannel> logger) : IAlertChannel
{
    public string Name => "Email";

    public async Task SendAsync(AlertNotification notification, CancellationToken ct = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.FromAddress) || string.IsNullOrWhiteSpace(opts.ToAddresses))
        {
            logger.LogWarning("Email não configurado — FromAddress ou ToAddresses ausente");
            return;
        }

        var recipients = opts.ToAddresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var emoji = notification.State == "resolved" ? "✅" : notification.Severity switch
        {
            AlertSeverity.Critical => "🔴",
            AlertSeverity.Warning => "🟡",
            _ => "🔵"
        };
        var status = notification.State == "resolved" ? "RESOLVIDO" : "ALERTA";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(opts.FromName, opts.FromAddress));
        foreach (var addr in recipients)
            message.To.Add(MailboxAddress.Parse(addr));

        message.Subject = $"{emoji} [{status}] {notification.Title}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildHtmlBody(notification, status, emoji),
            TextBody = $"[{status}] {notification.Title}\n\n{notification.Body}\n\nHorário: {notification.FiredAt:dd/MM/yyyy HH:mm:ss} UTC"
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(opts.SmtpHost, opts.SmtpPort, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(opts.Username, opts.Password, ct);
        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(true, ct);
    }

    private static string BuildHtmlBody(AlertNotification n, string status, string emoji)
    {
        var color = n.State == "resolved" ? "#16a34a" : n.Severity switch
        {
            AlertSeverity.Critical => "#dc2626",
            AlertSeverity.Warning => "#d97706",
            _ => "#2563eb"
        };

        var bodyLines = n.Body.Replace("\n", "<br/>");

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; background: #f3f4f6; padding: 24px;">
              <div style="max-width: 600px; margin: 0 auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,.1);">
                <div style="background: {color}; padding: 20px 24px;">
                  <h2 style="color: white; margin: 0;">{emoji} {status}: {n.Title}</h2>
                </div>
                <div style="padding: 24px;">
                  <p style="font-size: 15px; color: #374151; line-height: 1.6;">{bodyLines}</p>
                  <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;"/>
                  <p style="font-size: 13px; color: #9ca3af;">
                    🕐 Horário: {n.FiredAt:dd/MM/yyyy HH:mm:ss} UTC<br/>
                    Enviado pelo ECommerce Alert Service
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;
    }
}
