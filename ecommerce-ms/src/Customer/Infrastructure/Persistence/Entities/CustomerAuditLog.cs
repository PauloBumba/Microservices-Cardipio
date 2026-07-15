using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Logging.Categories;

namespace Customer.Infrastructure.Persistence.Entities;

/// <summary>
/// Entidade de auditoria para o serviço Customer.
/// Armazena todas as ações sensíveis realizadas no serviço.
/// </summary>
public class CustomerAuditLog
{
    public Guid Id { get; set; }
    public string TimestampUtc { get; set; } = string.Empty;
    public string Service { get; set; } = "customer-service";
    public string Environment { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ChangesJson { get; set; } = "{}";
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CustomerAuditLogConfiguration : IEntityTypeConfiguration<CustomerAuditLog>
{
    public void Configure(EntityTypeBuilder<CustomerAuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TimestampUtc).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Service).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Environment).IsRequired().HasMaxLength(50);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.UserName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Resource).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ResourceId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.ChangesJson).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(50);
        builder.Property(x => x.TraceId).HasMaxLength(50);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.Resource);
        builder.HasIndex(x => x.TimestampUtc);
        builder.HasIndex(x => x.CorrelationId);
    }
}
