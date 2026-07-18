using Microsoft.EntityFrameworkCore;
using Shared.Application.Auditing;

namespace Shared.Infrastructure.Logging.Repositories;

/// <summary>Implementação reutilizável que grava auditoria no banco do serviço atual.</summary>
public sealed class EfAuditRepository(DbContext dbContext) : Shared.Application.Auditing.IAuditRepository
{
    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<AuditEntry>().AddAsync(entry, cancellationToken);
    }
}
