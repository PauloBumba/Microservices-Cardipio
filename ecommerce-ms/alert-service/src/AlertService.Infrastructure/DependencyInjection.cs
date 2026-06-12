using AlertService.Core.Interfaces;
using AlertService.Infrastructure.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlertService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Telegram ──────────────────────────────────────────────────────────
        services.Configure<TelegramOptions>(configuration.GetSection("Channels:Telegram"));
        services.AddHttpClient<TelegramChannel>();
        services.AddSingleton<IAlertChannel, TelegramChannel>();

        // ── WhatsApp ──────────────────────────────────────────────────────────
        services.Configure<WhatsAppOptions>(configuration.GetSection("Channels:WhatsApp"));
        services.AddHttpClient<WhatsAppChannel>();
        services.AddSingleton<IAlertChannel, WhatsAppChannel>();

        // ── Email ─────────────────────────────────────────────────────────────
        services.Configure<EmailOptions>(configuration.GetSection("Channels:Email"));
        services.AddSingleton<IAlertChannel, EmailChannel>();

        return services;
    }
}
