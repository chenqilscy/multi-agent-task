using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Repository.Data.EntityTypeConfigurations
{
    /// <summary>
    /// SubTask 实体配置
    /// </summary>
    public class SubTaskConfiguration : IEntityTypeConfiguration<SubTask>
    {
        public void Configure(EntityTypeBuilder<SubTask> builder)
        {
            // 表名
            builder.ToTable("sub_tasks");

            // 主键
            builder.HasKey(s => s.Id);

            // 属性配置
            builder.Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Description)
                .HasMaxLength(1000);

            builder.Property(s => s.Status)
                .HasConversion<string>();

            builder.Property(s => s.MainTaskId)
                .IsRequired();

            // 索引
            builder.HasIndex(s => s.MainTaskId)
                .HasDatabaseName("idx_sub_tasks_main_task_id");

            builder.HasIndex(s => s.Status)
                .HasDatabaseName("idx_sub_tasks_status");

            builder.HasIndex(s => s.ExecutionOrder)
                .HasDatabaseName("idx_sub_tasks_execution_order");
        }
    }
}
