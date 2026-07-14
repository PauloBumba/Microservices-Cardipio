using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Behaviors;

/// <summary>
/// Centraliza captura de exceções no pipeline do MediatR.
/// Registra logs estruturados e relança exceções para tratamento pelo GlobalExceptionHandler.
/// Não altera o tratamento HTTP existente.
/// </summary>
public sealed class ExceptionBehavior<TRequest, TResponse>(
    ILogger<ExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;
            
            logger.LogError(ex, "[Exception] Erro ao processar {RequestName}: {Message}", requestName, ex.Message);
            
            // Relança para ser tratado pelo GlobalExceptionHandler no nível HTTP
            throw;
        }
    }
}
