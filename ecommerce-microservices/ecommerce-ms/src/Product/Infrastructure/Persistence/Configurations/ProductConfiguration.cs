using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Product.Infrastructure.Persistence.Configurations;
public sealed class ProductConfiguration : IEntityTypeConfiguration<Domain.Entities.Productss>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Productss> b)
    {
        b.ToTable("products"); b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnName("id");
        b.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(p => p.Description).HasColumnName("description").HasMaxLength(2000);
        b.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        b.Property(p => p.StockQuantity).HasColumnName("stock_quantity").IsRequired();
        b.Property(p => p.ReservedQuantity).HasColumnName("reserved_quantity").HasDefaultValue(0);
        b.Property(p => p.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
        b.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.OwnsOne(p => p.Price, m => {
            m.Property(x => x.Amount).HasColumnName("price_amount").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("price_currency").HasMaxLength(3).IsRequired();
        });
        b.Ignore(p => p.DomainEvents);
        b.Ignore(p => p.AvailableQuantity);
        b.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("ix_products_sku");
        b.HasIndex(p => p.Category).HasDatabaseName("ix_products_category");
    }
}
