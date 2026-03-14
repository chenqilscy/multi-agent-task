using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Repository.Data.EntityTypeConfigurations
{
    /// <summary>
    /// MainTask 实体配置
    /// </summary>
    public class MainTaskConfiguration : IEntityTypeConfiguration<MainTask>
    {
        public void Configure(EntityTypeBuilder<MainTask> builder)
        {
            // 表名
            builder.ToTable("main_tasks");

            // 主键
            builder.HasKey(m => m.Id);

            // 属性配置
            builder.Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Description)
                .HasMaxLength(2000);

            builder.Property(m => m.Priority)
                .HasConversion<int>();

            builder.Property(m => m.Status)
                .HasConversion<string>();

            builder.Property(m => m.CreatedAt)
                .IsRequired();

            // 索引
            builder.HasIndex(m => m.Status)
                .HasDatabaseName("idx_main_tasks_status");

            builder.HasIndex(m => m.Priority)
                .HasDatabaseName("idx_main_tasks_priority");

            builder.HasIndex(m => m.CreatedAt)
                .HasDatabaseName("idx_main_tasks_created_at");

            // 关系配置
            builder.HasMany(m => m.SubTasks)
                .WithOne(s => s.MainTask)
                .HasForeignKey(s => s.MainTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
