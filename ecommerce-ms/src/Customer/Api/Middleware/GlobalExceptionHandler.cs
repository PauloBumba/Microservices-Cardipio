using Customer.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace Customer.Api.Middleware;
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, TimeProvider timeProvider) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Unhandled: {Message}", ex.Message);
        var (status, title, detail) = ex switch
        {
            ValidationException ve    => (400, "Validation Error", string.Join(" | ", ve.Errors.Select(e => e.ErrorMessage))),
            CustomerNotFoundException  => (404, "Not Found",        ex.Message),
            CustomerDomainException    => (422, "Domain Error",     ex.Message),
            InvalidOperationException  => (409, "Conflict",         ex.Message),
            _                          => (500, "Internal Error",   "Unexpected error.")
        };
        var p = new ProblemDetails { Status=status, Title=title, Detail=detail, Instance=ctx.Request.Path };
        p.Extensions["traceId"]   = ctx.TraceIdentifier;
        p.Extensions["timestamp"] = timeProvider.GetUtcNow().UtcDateTime;
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(p, ct);
        return true;
    }
}
