using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shared.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowThresholdMs)
            logger.LogWarning("[Performance] {Request} demorou {Ms}ms — acima do threshold de {Threshold}ms",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, SlowThresholdMs);

        return response;
    }
}
