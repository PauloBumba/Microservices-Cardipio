namespace Shared.Application.Auditing;

public interface IAuditRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
