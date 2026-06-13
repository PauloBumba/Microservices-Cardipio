using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Customer.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    // Threshold para logar como Warning — ajuste conforme SLO do sistema
    private const int SlowThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var response = await next();

        sw.Stop();

        var elapsed = sw.ElapsedMilliseconds;
        var name = typeof(TRequest).Name;

        if (elapsed > SlowThresholdMs)
        {
            // Warning: visível em produção — indica gargalo real
            logger.LogWarning("[Performance] LENTO {Request}: {Time}ms (threshold: {Threshold}ms)",
                name, elapsed, SlowThresholdMs);
        }
        else
        {
            // Debug: não aparece em produção com nível padrão Information.
            // Antes era LogInformation — em produção com alta carga gerava
            // milhares de linhas de log por minuto sem valor diagnóstico.
            logger.LogDebug("[Performance] {Request}: {Time}ms", name, elapsed);
        }

        return response;
    }
}