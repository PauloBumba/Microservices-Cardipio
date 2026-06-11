using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;
namespace Order.Infrastructure.Persistence.Configurations;
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order.Domain.Entities.Orderss>
{
    public void Configure(EntityTypeBuilder<Order.Domain.Entities.Orderss> b)
    {
        b.ToTable("orders"); b.HasKey(o => o.Id);
        b.Property(o => o.Id).HasColumnName("id");
        b.Property(o => o.OrderNumber).HasColumnName("order_number").HasMaxLength(50).IsRequired();
        b.Property(o => o.CustomerId).HasColumnName("customer_id").IsRequired();
        b.Property(o => o.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        b.Property(o => o.Notes).HasColumnName("notes").HasMaxLength(500);
        b.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(o => o.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.OwnsOne(o => o.ShippingAddress, a => {
            a.Property(x => x.Street).HasColumnName("shipping_street").HasMaxLength(300).IsRequired();
            a.Property(x => x.City).HasColumnName("shipping_city").HasMaxLength(100).IsRequired();
            a.Property(x => x.State).HasColumnName("shipping_state").HasMaxLength(100).IsRequired();
            a.Property(x => x.ZipCode).HasColumnName("shipping_zip_code").HasMaxLength(20).IsRequired();
            a.Property(x => x.Country).HasColumnName("shipping_country").HasMaxLength(100).IsRequired();
        });
        b.OwnsOne(o => o.TotalAmount, m => {
            m.Property(x => x.Amount).HasColumnName("total_amount").HasPrecision(18,2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("total_currency").HasMaxLength(3).IsRequired();
        });
        b.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Ignore(o => o.DomainEvents);
        b.HasIndex(o => o.OrderNumber).IsUnique().HasDatabaseName("ix_orders_order_number");
        b.HasIndex(o => o.CustomerId).HasDatabaseName("ix_orders_customer_id");
        b.HasIndex(o => o.Status).HasDatabaseName("ix_orders_status");
    }
}
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("order_items"); b.HasKey(i => i.Id);
        b.Property(i => i.Id).HasColumnName("id");
        b.Property(i => i.OrderId).HasColumnName("order_id").IsRequired();
        b.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
        b.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        b.Property(i => i.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        b.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
        b.Property(i => i.UnitPrice).HasColumnName("unit_price").HasPrecision(18,2).IsRequired();
        b.Property(i => i.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        b.Ignore(i => i.TotalPrice);
        b.Ignore(i => i.DomainEvents);
        b.HasIndex(i => i.ProductId).HasDatabaseName("ix_order_items_product_id");
    }
}
