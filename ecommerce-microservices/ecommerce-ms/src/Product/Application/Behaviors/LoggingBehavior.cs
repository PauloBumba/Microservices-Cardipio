using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
namespace Product.Application.Behaviors;
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest,TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest req, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("→ {Req}", name);
        var sw = Stopwatch.StartNew();
        var r = await next();
        logger.LogInformation("← {Req} {Ms}ms", name, sw.ElapsedMilliseconds);
        return r;
    }
}
