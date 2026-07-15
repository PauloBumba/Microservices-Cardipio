using Customer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Logging.Repositories;

namespace Customer.Infrastructure.Persistence.Repositories;

public class CustomerAuditRepository(CustomerDbContext dbContext) : IAuditRepository
{
    public async Task AddAsync<TAuditLog>(TAuditLog auditLog, CancellationToken cancellationToken = default) 
        where TAuditLog : class
    {
        if (auditLog is CustomerAuditLog customerAuditLog)
        {
            await dbContext.AuditLogs.AddAsync(customerAuditLog, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<TAuditLog>> GetByUserIdAsync<TAuditLog>(string userId, CancellationToken cancellationToken = default) 
        where TAuditLog : class
    {
        if (typeof(TAuditLog) == typeof(CustomerAuditLog))
        {
            var logs = await dbContext.AuditLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            
            return logs.Cast<TAuditLog>();
        }
        
        return Enumerable.Empty<TAuditLog>();
    }

    public async Task<IEnumerable<TAuditLog>> GetByDateRangeAsync<TAuditLog>(DateTime start, DateTime end, CancellationToken cancellationToken = default) 
        where TAuditLog : class
    {
        if (typeof(TAuditLog) == typeof(CustomerAuditLog))
        {
            var logs = await dbContext.AuditLogs
                .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            
            return logs.Cast<TAuditLog>();
        }
        
        return Enumerable.Empty<TAuditLog>();
    }

    public async Task<IEnumerable<TAuditLog>> GetByActionAsync<TAuditLog>(string action, CancellationToken cancellationToken = default) 
        where TAuditLog : class
    {
        if (typeof(TAuditLog) == typeof(CustomerAuditLog))
        {
            var logs = await dbContext.AuditLogs
                .Where(x => x.Action == action)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            
            return logs.Cast<TAuditLog>();
        }
        
        return Enumerable.Empty<TAuditLog>();
    }

    public async Task<IEnumerable<TAuditLog>> GetByResourceAsync<TAuditLog>(string resource, CancellationToken cancellationToken = default) 
        where TAuditLog : class
    {
        if (typeof(TAuditLog) == typeof(CustomerAuditLog))
        {
            var logs = await dbContext.AuditLogs
                .Where(x => x.Resource == resource)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            
            return logs.Cast<TAuditLog>();
        }
        
        return Enumerable.Empty<TAuditLog>();
    }
}
