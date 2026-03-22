using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.EntityConfigurations;

public class UserBehaviorRecordEntityConfiguration : IEntityTypeConfiguration<UserBehaviorRecordEntity>
{
    public void Configure(EntityTypeBuilder<UserBehaviorRecordEntity> builder)
    {
        builder.ToTable("cs_user_behavior_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.SessionId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Intent)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.EntitiesJson)
            .HasMaxLength(2000);

        builder.Property(r => r.Timestamp)
            .IsRequired();

        // 索引
        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("idx_cs_user_behavior_user_id");

        builder.HasIndex(r => r.Intent)
            .HasDatabaseName("idx_cs_user_behavior_intent");

        builder.HasIndex(r => r.Timestamp)
            .HasDatabaseName("idx_cs_user_behavior_timestamp");
    }
}
