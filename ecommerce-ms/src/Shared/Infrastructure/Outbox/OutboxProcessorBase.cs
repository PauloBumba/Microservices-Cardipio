using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Shared.Infrastructure.Outbox;

/// <summary>
/// Base reutilizável para o OutboxProcessor de cada microsserviço.
/// Implemente GetPendingAsync e MarkProcessed/IncrementRetry no serviço concreto.
/// Fornece: backoff exponencial, lock otimista, idempotência via ProcessedEvent.
/// </summary>
public abstract class OutboxProcessorBase(
    IServiceProvider services,
    ILogger logger) : BackgroundService
{
    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(16),
        TimeSpan.FromSeconds(32),
    ];
    private const int MaxRetries = 5;
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("[Outbox] {Processor} iniciado.", GetType().Name);
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(ct); }
            catch (Exception ex) { logger.LogError(ex, "[Outbox] Erro inesperado no {Processor}", GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await GetPendingAsync(scope.ServiceProvider, BatchSize, ct);
        if (messages.Count == 0) return;

        logger.LogDebug("[Outbox] Processando {Count} mensagens.", messages.Count);

        foreach (var msg in messages)
        {
            // Lock otimista — marca como em processamento
            if (!await TryLockAsync(scope.ServiceProvider, msg.Id, ct)) continue;

            try
            {
                // Idempotência — verifica se já foi processado
                if (await WasProcessedAsync(scope.ServiceProvider, msg.Id, ct))
                {
                    await MarkProcessedAsync(scope.ServiceProvider, msg, ct);
                    continue;
                }

                var type = Type.GetType(msg.Type);
                if (type is null)
                {
                    logger.LogWarning("[Outbox] Tipo não encontrado: {Type}", msg.Type);
                    await IncrementRetryAsync(scope.ServiceProvider, msg, "Tipo não encontrado", ct);
                    continue;
                }

                var payload = JsonSerializer.Deserialize(msg.Payload, type)!;
                await publisher.Publish(payload, ct);

                await MarkProcessedAsync(scope.ServiceProvider, msg, ct);
                await RecordProcessedEventAsync(scope.ServiceProvider, msg.Id, msg.Type, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Outbox] Falha ao processar mensagem {Id}", msg.Id);
                await IncrementRetryWithBackoffAsync(scope.ServiceProvider, msg, ex.Message, ct);
            }
        }
    }

    private async Task IncrementRetryWithBackoffAsync(IServiceProvider sp, OutboxMessage msg, string error, CancellationToken ct)
    {
        msg.RetryCount++;
        if (msg.RetryCount >= MaxRetries)
        {
            msg.Status = OutboxMessageStatus.DeadLetter;
            logger.LogError("[Outbox] Mensagem {Id} enviada para DeadLetter após {Max} tentativas.", msg.Id, MaxRetries);
        }

        var delay = BackoffDelays[Math.Min(msg.RetryCount - 1, BackoffDelays.Length - 1)];
        logger.LogWarning("[Outbox] Retry {Count}/{Max} para {Id} — aguardando {Delay}s", msg.RetryCount, MaxRetries, msg.Id, delay.TotalSeconds);
        await Task.Delay(delay, ct);
        await IncrementRetryAsync(sp, msg, error, ct);
    }

    // ── Abstrações que cada microsserviço implementa ───────────────────────
    protected abstract Task<List<OutboxMessage>> GetPendingAsync(IServiceProvider sp, int batchSize, CancellationToken ct);
    protected abstract Task MarkProcessedAsync(IServiceProvider sp, OutboxMessage msg, CancellationToken ct);
    protected abstract Task IncrementRetryAsync(IServiceProvider sp, OutboxMessage msg, string error, CancellationToken ct);
    protected abstract Task<bool> TryLockAsync(IServiceProvider sp, Guid messageId, CancellationToken ct);
    protected abstract Task<bool> WasProcessedAsync(IServiceProvider sp, Guid eventId, CancellationToken ct);
    protected abstract Task RecordProcessedEventAsync(IServiceProvider sp, Guid eventId, string type, CancellationToken ct);
}
