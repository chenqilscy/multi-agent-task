using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data.EntityConfigurations;

public class FaqEntryEntityConfiguration : IEntityTypeConfiguration<FaqEntryEntity>
{
    public void Configure(EntityTypeBuilder<FaqEntryEntity> builder)
    {
        builder.ToTable("cs_faq_entries");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Question)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.Answer)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(f => f.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.KeywordsJson)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // 索引
        builder.HasIndex(f => f.Category)
            .HasDatabaseName("idx_cs_faq_entries_category");

        builder.HasIndex(f => f.IsActive)
            .HasDatabaseName("idx_cs_faq_entries_is_active");
    }
}
