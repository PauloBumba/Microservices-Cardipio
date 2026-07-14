using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Behaviors;

/// <summary>
/// Placeholder para AuthorizationBehavior.
/// 
/// TODO: Integrar com microserviço de autenticação/autorização quando disponível.
/// 
/// Estrutura preparada para:
/// - Verificação de permissões baseadas em roles/claims
/// - Validação de acesso a recursos específicos
/// - Integração com JWT tokens
/// - Policy-based authorization
/// 
/// IMPORTANTE: Atualmente não bloqueia nenhuma requisição.
/// Quando o microserviço de auth estiver disponível, implementar:
/// - IAuthorizationService para verificar permissões
/// - IPermissionProvider para obter permissões do usuário
/// - Cache de permissões para performance
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(
    ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // TODO: Implementar verificação de autorização quando o microserviço de auth estiver disponível
        // Exemplo futuro:
        // var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        // var requiredPermissions = typeof(TRequest).GetCustomAttributes<RequirePermissionAttribute>();
        // if (!await _authorizationService.HasPermissionsAsync(userId, requiredPermissions, ct))
        //     throw new UnauthorizedAccessException();

        logger.LogDebug("[Authorization] Placeholder - não implementado ainda");
        return await next();
    }
}
