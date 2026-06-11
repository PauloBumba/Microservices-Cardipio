using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Customer.Infrastructure.Persistence.Configurations;
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Domain.Entities.Customerss>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Customerss> b)
    {
        b.ToTable("customers");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnName("id");
        b.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.OwnsOne(c => c.Email, e => {
            e.Property(x => x.Value).HasColumnName("email").HasMaxLength(320).IsRequired();
            e.HasIndex(x => x.Value).IsUnique().HasDatabaseName("ix_customers_email");
        });
        b.OwnsOne(c => c.Phone, p => p.Property(x => x.Value).HasColumnName("phone").HasMaxLength(30).IsRequired());
        b.OwnsOne(c => c.Address, a => {
            a.Property(x => x.Street).HasColumnName("street").HasMaxLength(300).IsRequired();
            a.Property(x => x.City).HasColumnName("city").HasMaxLength(100).IsRequired();
            a.Property(x => x.State).HasColumnName("state").HasMaxLength(100).IsRequired();
            a.Property(x => x.ZipCode).HasColumnName("zip_code").HasMaxLength(20).IsRequired();
            a.Property(x => x.Country).HasColumnName("country").HasMaxLength(100).IsRequired();
        });
        b.Ignore(c => c.DomainEvents);
        b.HasIndex(c => c.IsActive).HasDatabaseName("ix_customers_is_active");
    }
}
