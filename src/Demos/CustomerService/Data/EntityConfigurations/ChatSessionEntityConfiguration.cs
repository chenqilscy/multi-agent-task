using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.EntityConfigurations;

public class ChatSessionEntityConfiguration : IEntityTypeConfiguration<ChatSessionEntity>
{
    public void Configure(EntityTypeBuilder<ChatSessionEntity> builder)
    {
        builder.ToTable("cs_chat_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SessionId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.StartedAt)
            .IsRequired();

        builder.Property(s => s.Summary)
            .HasMaxLength(2000);

        // 索引
        builder.HasIndex(s => s.SessionId)
            .IsUnique()
            .HasDatabaseName("idx_cs_chat_sessions_session_id");

        builder.HasIndex(s => s.CustomerId)
            .HasDatabaseName("idx_cs_chat_sessions_customer_id");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("idx_cs_chat_sessions_status");

        builder.HasIndex(s => s.StartedAt)
            .HasDatabaseName("idx_cs_chat_sessions_started_at");

        // 关系
        builder.HasMany(s => s.Messages)
            .WithOne(m => m.ChatSession)
            .HasForeignKey(m => m.ChatSessionEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatMessageEntityConfiguration : IEntityTypeConfiguration<ChatMessageEntity>
{
    public void Configure(EntityTypeBuilder<ChatMessageEntity> builder)
    {
        builder.ToTable("cs_chat_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.Intent)
            .HasMaxLength(50);

        builder.Property(m => m.EntitiesJson)
            .HasMaxLength(2000);

        builder.Property(m => m.Timestamp)
            .IsRequired();

        // 索引
        builder.HasIndex(m => m.ChatSessionEntityId)
            .HasDatabaseName("idx_cs_chat_messages_session_id");

        builder.HasIndex(m => m.Timestamp)
            .HasDatabaseName("idx_cs_chat_messages_timestamp");
    }
}
