// ─────────────────────────────────────────────────────────────────────────────
// Arquivo: src/<Servico>/Infrastructure/Outbox/OutboxMetricsCollector.cs
//
// BackgroundService que roda a cada 10s e atualiza os Gauges do Prometheus
// com o estado atual do Outbox no banco de dados.
//
// COMO USAR:
//   1. Copie este arquivo para cada serviço ajustando o DbContext
//   2. No DependencyInjection.cs do serviço, adicione:
//      services.AddHostedService<OutboxMetricsCollector>();
//
// Exemplo abaixo usa OrderDbContext — ajuste para CustomerDbContext, etc.
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence; // <- troque para o DbContext correto
using Shared.Metrics;

namespace Order.Infrastructure.Outbox; // <- ajuste o namespace

public sealed class OutboxMetricsCollector(
    IServiceProvider services,
    ILogger<OutboxMetricsCollector> logger) : BackgroundService
{
    // Nome do serviço usado na label do Prometheus
    private const string ServiceName = "order-service"; // <- troque por cada serviço

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("OutboxMetricsCollector iniciado para {Service}", ServiceName);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CollectAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Erro ao coletar métricas do Outbox");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }

    private async Task CollectAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>(); // <- troque aqui

        var pending = await db.OutboxMessages
            .CountAsync(m => m.ProcessedAt == null && m.RetryCount < 5, ct);

        var failed = await db.OutboxMessages
            .CountAsync(m => m.ProcessedAt == null && m.RetryCount >= 5, ct);

        BusinessMetrics.OutboxPending.WithLabels(ServiceName).Set(pending);
        BusinessMetrics.OutboxFailed.WithLabels(ServiceName).Set(failed);
    }
}
