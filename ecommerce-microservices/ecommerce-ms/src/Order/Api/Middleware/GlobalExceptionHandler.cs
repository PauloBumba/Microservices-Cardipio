using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Order.Domain.Exceptions;
using Polly.CircuitBreaker;
namespace Order.Api.Middleware;
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Unhandled: {Message}", ex.Message);
        var (status, title, detail) = ex switch
        {
            ValidationException ve    => (400, "Validation Error",    string.Join(" | ", ve.Errors.Select(e => e.ErrorMessage))),
            OrderNotFoundException     => (404, "Not Found",           ex.Message),
            OrderDomainException       => (422, "Domain Error",        ex.Message),
            BrokenCircuitException     => (503, "Service Unavailable", "Product Service temporariamente indisponível."),
            InvalidOperationException  => (409, "Conflict",            ex.Message),
            _                          => (500, "Internal Error",      "Erro inesperado.")
        };
        var p = new ProblemDetails { Status=status, Title=title, Detail=detail, Instance=ctx.Request.Path };
        p.Extensions["traceId"] = ctx.TraceIdentifier;
        p.Extensions["timestamp"] = DateTime.UtcNow;
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(p, ct);
        return true;
    }
}
