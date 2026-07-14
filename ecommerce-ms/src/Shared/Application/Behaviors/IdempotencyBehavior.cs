using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Behaviors;

/// <summary>
/// Interface para comandos que suportam idempotency.
/// Implemente em Commands que devem garantir execução única.
/// </summary>
public interface IIdempotentCommand
{
    string IdempotencyKey { get; }
}

/// <summary>
/// Placeholder para IdempotencyBehavior.
/// 
/// TODO: Implementar cache de resultados por IdempotencyKey quando necessário.
/// 
/// Estrutura preparada para:
/// - Cache de resultados baseado em IdempotencyKey
/// - Verificação de duplicatas antes de executar handler
/// - Retorno de resultado cacheado para requisições duplicadas
/// - TTL configurável para cache de idempotency
/// 
/// Fluxo futuro:
/// 1. Verificar se IdempotencyKey existe no cache
/// 2. Se existir, retornar resultado cacheado
/// 3. Se não existir, executar handler
/// 4. Salvar resultado no cache com TTL
/// 
/// IMPORTANTE: Atualmente não implementa cache.
/// Quando necessário, implementar com IDistributedCache.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // TODO: Implementar cache de idempotency quando necessário
        // Exemplo futuro:
        // if (request is IIdempotentCommand idempotent)
        // {
        //     var cacheKey = $"idempotent:{idempotent.IdempotencyKey}";
        //     var cached = await _cache.GetAsync<TResponse>(cacheKey, ct);
        //     if (cached is not null)
        //     {
        //         logger.LogDebug("[Idempotency] Cache HIT for key: {Key}", idempotent.IdempotencyKey);
        //         return cached;
        //     }
        //     
        //     var response = await next();
        //     await _cache.SetAsync(cacheKey, response, TimeSpan.FromHours(24), ct);
        //     return response;
        // }

        logger.LogDebug("[Idempotency] Placeholder - não implementado ainda");
        return await next();
    }
}
