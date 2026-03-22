using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.EntityConfigurations;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("cs_customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("zh-CN");

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // 索引
        builder.HasIndex(c => c.CustomerId)
            .IsUnique()
            .HasDatabaseName("idx_cs_customers_customer_id");

        builder.HasIndex(c => c.Email)
            .HasDatabaseName("idx_cs_customers_email");

        // 关系
        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Tickets)
            .WithOne(t => t.Customer)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.ChatSessions)
            .WithOne(s => s.Customer)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
