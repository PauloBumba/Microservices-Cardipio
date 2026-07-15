namespace Shared.Infrastructure.Logging.Repositories;

/// <summary>
/// Repositório para persistência de logs de auditoria.
/// Cada serviço deve implementar sua própria versão com seu DbContext.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Adiciona um registro de auditoria de forma assíncrona.
    /// </summary>
    Task AddAsync<TAuditLog>(TAuditLog auditLog, CancellationToken cancellationToken = default) 
        where TAuditLog : class;

    /// <summary>
    /// Busca registros de auditoria por usuário.
    /// </summary>
    Task<IEnumerable<TAuditLog>> GetByUserIdAsync<TAuditLog>(string userId, CancellationToken cancellationToken = default) 
        where TAuditLog : class;

    /// <summary>
    /// Busca registros de auditoria por período.
    /// </summary>
    Task<IEnumerable<TAuditLog>> GetByDateRangeAsync<TAuditLog>(DateTime start, DateTime end, CancellationToken cancellationToken = default) 
        where TAuditLog : class;

    /// <summary>
    /// Busca registros de auditoria por ação.
    /// </summary>
    Task<IEnumerable<TAuditLog>> GetByActionAsync<TAuditLog>(string action, CancellationToken cancellationToken = default) 
        where TAuditLog : class;

    /// <summary>
    /// Busca registros de auditoria por recurso.
    /// </summary>
    Task<IEnumerable<TAuditLog>> GetByResourceAsync<TAuditLog>(string resource, CancellationToken cancellationToken = default) 
        where TAuditLog : class;
}
