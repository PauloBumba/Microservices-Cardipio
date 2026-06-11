using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;
namespace Notification.Infrastructure.Persistence.Configurations;
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification.Domain.Entities.NotificationS>
{
    public void Configure(EntityTypeBuilder<Notification.Domain.Entities.NotificationS> b)
    {
        b.ToTable("notifications"); b.HasKey(n => n.Id);
        b.Property(n => n.Type).HasMaxLength(100).IsRequired();
        b.Property(n => n.Recipient).HasMaxLength(320).IsRequired();
        b.Property(n => n.Channel).HasMaxLength(50).IsRequired();
        b.Property(n => n.Subject).HasMaxLength(500).IsRequired();
        b.Property(n => n.Body).HasColumnType("text").IsRequired();
        b.Property(n => n.Status).HasConversion<string>().IsRequired();
        b.Property(n => n.Error).HasMaxLength(2000);
        b.Property(n => n.RetryCount).HasDefaultValue(0);
        b.Property(n => n.CreatedAt).IsRequired();
        b.Property(n => n.SentAt);
        b.HasIndex(n => n.Status).HasDatabaseName("ix_notifications_status");
        b.HasIndex(n => n.Recipient).HasDatabaseName("ix_notifications_recipient");
        b.HasIndex(n => n.CreatedAt).HasDatabaseName("ix_notifications_created_at");
    }
}
