using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Auditing;
using System.Diagnostics;
using System.Security.Claims;

namespace Shared.Application.Behaviors;

public interface IAuditableCommand
{
    string AuditAction { get; }
    string AuditResource { get; }
    string? AuditResourceId { get; }
}

/// <summary>Persiste o evento localmente e o encaminha para o log centralizado.</summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditRepository auditRepository,
    IAuditLogger auditLogger,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IAuditableCommand command)
            return await next();

        try
        {
            var response = await next();
            await RecordAsync(command, true, null, ct);
            return response;
        }
        catch (Exception exception)
        {
            await RecordAsync(command, false, exception.Message, ct);
            throw;
        }
    }

    private async Task RecordAsync(IAuditableCommand command, bool success, string? errorMessage, CancellationToken ct)
    {
        var context = httpContextAccessor.HttpContext;
        var user = context?.User;
        var entry = new AuditEntry
        {
            Service = AppDomain.CurrentDomain.FriendlyName,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub"),
            UserName = user?.Identity?.Name,
            Action = command.AuditAction,
            Resource = command.AuditResource,
            ResourceId = command.AuditResourceId,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            Success = success,
            ErrorMessage = errorMessage,
            CorrelationId = context?.TraceIdentifier,
            TraceId = Activity.Current?.TraceId.ToString()
        };

        try
        {
            await auditRepository.AddAsync(entry, ct);
            await auditLogger.LogAsync(entry, ct);
        }
        catch (Exception auditException)
        {
            logger.LogError(auditException, "Falha ao registrar auditoria de {Action} em {Resource}", entry.Action, entry.Resource);
        }
    }
}
