using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.EntityConfigurations;

public class TicketEntityConfiguration : IEntityTypeConfiguration<TicketEntity>
{
    public void Configure(EntityTypeBuilder<TicketEntity> builder)
    {
        builder.ToTable("cs_tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.Category)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.RelatedOrderId)
            .HasMaxLength(50);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // 索引
        builder.HasIndex(t => t.TicketId)
            .IsUnique()
            .HasDatabaseName("idx_cs_tickets_ticket_id");

        builder.HasIndex(t => t.CustomerId)
            .HasDatabaseName("idx_cs_tickets_customer_id");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("idx_cs_tickets_status");

        builder.HasIndex(t => t.Priority)
            .HasDatabaseName("idx_cs_tickets_priority");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("idx_cs_tickets_created_at");

        // 关系
        builder.HasMany(t => t.Comments)
            .WithOne(c => c.Ticket)
            .HasForeignKey(c => c.TicketEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TicketCommentEntityConfiguration : IEntityTypeConfiguration<TicketCommentEntity>
{
    public void Configure(EntityTypeBuilder<TicketCommentEntity> builder)
    {
        builder.ToTable("cs_ticket_comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Author)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.CreatedAt)
            .IsRequired();
    }
}
