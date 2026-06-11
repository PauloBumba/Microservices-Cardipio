using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Infrastructure.Outbox;
namespace Product.Infrastructure.Persistence.Configurations;
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages"); b.HasKey(m => m.Id);
        b.Property(m => m.Type).HasMaxLength(500).IsRequired();
        b.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        b.Property(m => m.CreatedAt).IsRequired();
        b.Property(m => m.Error).HasMaxLength(2000);
        b.Property(m => m.RetryCount).HasDefaultValue(0);
        b.HasIndex(m => new { m.ProcessedAt, m.RetryCount })
            .HasDatabaseName("ix_outbox_unprocessed").HasFilter("processed_at IS NULL AND retry_count < 5");
    }
}
