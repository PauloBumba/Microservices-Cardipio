using AlertService.Core.Interfaces;
using AlertService.Core.Services;
using AlertService.Infrastructure.AI;
using AlertService.Infrastructure.Channels;
using AlertService.Infrastructure.Clients;
using AlertService.Infrastructure.MCP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlertService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Canais de notificação ─────────────────────────────────────────────
        services.Configure<TelegramOptions>(configuration.GetSection("Channels:Telegram"));
        services.AddHttpClient<TelegramChannel>();
        services.AddSingleton<IAlertChannel, TelegramChannel>();

        services.Configure<WhatsAppOptions>(configuration.GetSection("Channels:WhatsApp"));
        services.AddHttpClient<WhatsAppChannel>();
        services.AddSingleton<IAlertChannel, WhatsAppChannel>();

        services.Configure<EmailOptions>(configuration.GetSection("Channels:Email"));
        services.AddSingleton<IAlertChannel, EmailChannel>();

        // ── Feature Flags ─────────────────────────────────────────────────────
        services.AddSingleton<FeatureFlags>();

        // ── MCP (Grafana → Loki + Prometheus) ─────────────────────────────────
        services.Configure<McpOptions>(configuration.GetSection("Mcp"));
        services.AddSingleton<IMcpObservabilityClient, GrafanaMcpClient>();
        services.AddSingleton<IRecentIncidentStore, RecentIncidentStore>();

        // ── HTTP direto: Loki ─────────────────────────────────────────────────
        services.Configure<LokiOptions>(configuration.GetSection("Enrichment:Loki"));
        services.AddHttpClient<LokiClient>(client =>
        {
            var timeout = configuration.GetValue<int>("Enrichment:Loki:TimeoutSec", 10);
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });
        services.AddSingleton<ILokiClient, LokiClient>();

        // ── HTTP direto: Prometheus ───────────────────────────────────────────
        services.Configure<PrometheusOptions>(configuration.GetSection("Enrichment:Prometheus"));
        services.AddHttpClient<PrometheusClient>(client =>
        {
            var timeout = configuration.GetValue<int>("Enrichment:Prometheus:TimeoutSec", 10);
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });
        services.AddSingleton<IPrometheusClient, PrometheusClient>();

        // ── Ollama AI ─────────────────────────────────────────────────────────
        services.Configure<OllamaOptions>(configuration.GetSection("Enrichment:Ollama"));
        services.AddHttpClient<OllamaAiService>(client =>
        {
            var timeout = configuration.GetValue<int>("Enrichment:Ollama:TimeoutSec", 30);
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });
        services.AddSingleton<IIncidentAiService, OllamaAiService>();

        // ── Incident Enricher ─────────────────────────────────────────────────
        services.AddSingleton<IIncidentEnricher, IncidentEnricher>();

        return services;
    }
}
