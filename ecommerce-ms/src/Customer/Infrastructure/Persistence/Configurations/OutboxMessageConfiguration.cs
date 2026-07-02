using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Outbox;

namespace Customer.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id");

        b.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(500)
            .IsRequired();

        b.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        b.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        b.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        b.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        b.HasIndex(x => new { x.ProcessedAt, x.RetryCount })
            .HasDatabaseName("ix_outbox_unprocessed")
            .HasFilter("processed_at IS NULL AND retry_count < 5");
    }
}