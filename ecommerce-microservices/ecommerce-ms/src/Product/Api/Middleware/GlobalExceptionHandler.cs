using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Product.Domain.Exceptions;
namespace Product.Api.Middleware;
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Unhandled: {Message}", ex.Message);
        var (status, title, detail) = ex switch
        {
            ValidationException ve     => (400, "Validation Error", string.Join(" | ", ve.Errors.Select(e => e.ErrorMessage))),
            ProductNotFoundException    => (404, "Not Found",        ex.Message),
            InsufficientStockException  => (409, "Conflict",         ex.Message),
            ProductDomainException      => (422, "Domain Error",     ex.Message),
            InvalidOperationException   => (409, "Conflict",         ex.Message),
            _                           => (500, "Internal Error",   "Unexpected error.")
        };
        var p = new ProblemDetails { Status=status, Title=title, Detail=detail, Instance=ctx.Request.Path };
        p.Extensions["traceId"] = ctx.TraceIdentifier;
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(p, ct);
        return true;
    }
}
