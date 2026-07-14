using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Response;

namespace Shared.Application.Behaviors;

/// <summary>
/// Commita a UoW automaticamente após o handler, mas APENAS se a resposta
/// for ApiResponse com IsSuccess=true. Handlers não devem chamar CommitAsync diretamente.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWorkAccessor uow,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogDebug("[TX] Iniciando {Request}", typeof(TRequest).Name);
        var response = await next();

        if (response is IApiResponseMarker apiResponse && apiResponse.IsSuccess == false)
        {
            logger.LogDebug("[TX] Resposta com falha — commit cancelado para {Request}", typeof(TRequest).Name);
            return response;
        }

        await uow.CommitAsync(ct);
        logger.LogDebug("[TX] Commit OK para {Request}", typeof(TRequest).Name);
        return response;
    }
}
