using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogDebug("[Pipeline] → {Request}", name);
        try
        {
            var response = await next();
            logger.LogDebug("[Pipeline] ← {Request}", name);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Pipeline] Exceção em {Request}", name);
            throw;
        }
    }
}
