using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shared.Application.Behaviors;

/// <summary>
/// Integra observabilidade com OpenTelemetry já configurado no projeto.
/// Cria Activity spans automaticamente para cada request do MediatR.
/// Registra TraceId, SpanId, Duration e logs estruturados.
/// </summary>
public sealed class ObservabilityBehavior<TRequest, TResponse>(
    ILogger<ObservabilityBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource Source = new ActivitySource("MediatR");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        
        using var activity = Source.StartActivity(requestName);
        
        if (activity != null)
        {
            activity.SetTag("mediatr.request", requestName);
            activity.SetTag("mediatr.type", typeof(TRequest).IsAssignableTo(typeof(IBaseCommand)) ? "Command" : "Query");
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogDebug("[Observability] Iniciando {RequestName} (TraceId: {TraceId}, SpanId: {SpanId})",
                requestName,
                activity?.TraceId,
                activity?.SpanId);

            var response = await next();

            stopwatch.Stop();

            if (activity != null)
            {
                activity.SetTag("mediatr.success", true);
                activity.SetTag("mediatr.duration_ms", stopwatch.ElapsedMilliseconds);
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            logger.LogDebug("[Observability] {RequestName} completado em {Duration}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (activity != null)
            {
                activity.SetTag("mediatr.success", false);
                activity.SetTag("mediatr.duration_ms", stopwatch.ElapsedMilliseconds);
                activity.SetTag("mediatr.error", ex.Message);
                activity.SetTag("mediatr.exception_type", ex.GetType().Name);
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }

            logger.LogError(ex, "[Observability] {RequestName} falhou após {Duration}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
