using Microsoft.EntityFrameworkCore;
using Shared.Application.Auditing;

namespace Shared.Infrastructure.Logging.Repositories;

public static class AuditEntryModelBuilderExtensions
{
    public static void ConfigureAuditEntries(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Service).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Environment).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Resource).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserId).HasMaxLength(200);
            builder.Property(x => x.UserName).HasMaxLength(200);
            builder.Property(x => x.ResourceId).HasMaxLength(200);
            builder.Property(x => x.IpAddress).HasMaxLength(50);
            builder.Property(x => x.UserAgent).HasMaxLength(500);
            builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
            builder.Property(x => x.CorrelationId).HasMaxLength(100);
            builder.Property(x => x.TraceId).HasMaxLength(100);
            builder.HasIndex(x => x.TimestampUtc);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Action);
            builder.HasIndex(x => x.Resource);
            builder.HasIndex(x => x.CorrelationId);
        });
    }
}
