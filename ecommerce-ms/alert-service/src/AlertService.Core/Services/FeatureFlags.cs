using Microsoft.Extensions.Configuration;

namespace AlertService.Core.Services;

/// <summary>
/// Feature flags lidas do appsettings/env. Imutável após startup.
/// </summary>
public sealed class FeatureFlags
{
    public bool AiEnabled { get; }
    public bool LokiEnrichmentEnabled { get; }
    public bool PrometheusEnrichmentEnabled { get; }
    public bool McpEnrichmentEnabled { get; }
    public bool McpServerEnabled { get; }
    public TimeSpan EnrichmentTimeout { get; }

    public FeatureFlags(IConfiguration config)
    {
        var section = config.GetSection("FeatureFlags");
        AiEnabled                   = section.GetValue<bool>("AiEnabled");
        LokiEnrichmentEnabled       = section.GetValue<bool>("LokiEnrichmentEnabled");
        PrometheusEnrichmentEnabled = section.GetValue<bool>("PrometheusEnrichmentEnabled");
        McpEnrichmentEnabled        = section.GetValue<bool>("McpEnrichmentEnabled");
        McpServerEnabled            = section.GetValue<bool>("McpServerEnabled");
        EnrichmentTimeout           = TimeSpan.FromSeconds(
            section.GetValue<int>("EnrichmentTimeoutSeconds", 15));
    }
}
