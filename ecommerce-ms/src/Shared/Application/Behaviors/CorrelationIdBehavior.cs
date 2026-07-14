using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Shared.Application.Behaviors;

/// <summary>
/// Garante que CorrelationId esteja presente no contexto de logs e OpenTelemetry.
/// Se não existir no header, gera automaticamente.
/// </summary>
public sealed class CorrelationIdBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CorrelationIdBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var correlationId = httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
        }

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            logger.LogDebug("[CorrelationId] {CorrelationId}", correlationId);
            var response = await next();
            return response;
        }
    }
}
