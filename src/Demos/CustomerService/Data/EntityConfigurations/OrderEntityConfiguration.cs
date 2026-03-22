using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.EntityConfigurations;

public class OrderEntityConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("cs_orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(o => o.TrackingNumber)
            .HasMaxLength(50);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        // 索引
        builder.HasIndex(o => o.OrderId)
            .IsUnique()
            .HasDatabaseName("idx_cs_orders_order_id");

        builder.HasIndex(o => o.CustomerId)
            .HasDatabaseName("idx_cs_orders_customer_id");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("idx_cs_orders_status");

        builder.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("idx_cs_orders_created_at");

        // 关系
        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemEntityConfiguration : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> builder)
    {
        builder.ToTable("cs_order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2);
    }
}

public class TrackingEventEntityConfiguration : IEntityTypeConfiguration<TrackingEventEntity>
{
    public void Configure(EntityTypeBuilder<TrackingEventEntity> builder)
    {
        builder.ToTable("cs_tracking_events");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TrackingNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Timestamp)
            .IsRequired();

        // 索引
        builder.HasIndex(t => t.TrackingNumber)
            .HasDatabaseName("idx_cs_tracking_events_tracking_number");

        builder.HasIndex(t => t.OrderEntityId)
            .HasDatabaseName("idx_cs_tracking_events_order_id");

        // 关系
        builder.HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
