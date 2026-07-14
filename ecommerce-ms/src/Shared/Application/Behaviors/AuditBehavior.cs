using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Behaviors;

/// <summary>
/// Interface para comandos que devem ser auditados.
/// Implemente em Commands que requerem rastreamento de auditoria.
/// </summary>
public interface IAuditableCommand
{
    string AuditAction { get; }
    string AuditResource { get; }
    string? AuditResourceId { get; }
}

/// <summary>
/// Placeholder para AuditBehavior.
/// 
/// TODO: Implementar persistência de auditoria quando necessário.
/// 
/// Estrutura preparada para:
/// - Registro de ações sensíveis (criação, atualização, exclusão)
/// - Captura de userId, timestamp, IP address
/// - Armazenamento em tabela dedicada de auditoria
/// - Integração com serviço de auditoria centralizado
/// - Exportação para compliance (SOX, ISO 27001, LGPD)
/// 
/// Fluxo futuro:
/// 1. Extrair informações do comando e contexto HTTP
/// 2. Criar registro de auditoria com todos os metadados
/// 3. Persistir em banco de dados dedicado
/// 4. Enviar para serviço de auditoria centralizado (opcional)
/// 
/// IMPORTANTE: Atualmente não implementa persistência.
/// Quando necessário, implementar com IAuditRepository e tabela dedicada.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // TODO: Implementar persistência de auditoria quando necessário
        // Exemplo futuro:
        // if (request is IAuditableCommand auditable)
        // {
        //     var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        //     var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        //     
        //     var auditLog = new AuditLog
        //     {
        //         Action = auditable.AuditAction,
        //         Resource = auditable.AuditResource,
        //         ResourceId = auditable.AuditResourceId,
        //         UserId = userId,
        //         IpAddress = ipAddress,
        //         TimestampUtc = DateTime.UtcNow
        //     };
        //     
        //     await _auditRepository.AddAsync(auditLog, ct);
        // }

        logger.LogDebug("[Audit] Placeholder - não implementado ainda");
        return await next();
    }
}
