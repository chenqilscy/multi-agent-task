using CKY.MultiAgentFramework.Core.Constants;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Repository.Data.EntityTypeConfigurations
{
    /// <summary>
    /// 调度计划实体配置
    /// </summary>
    public class SchedulePlanConfiguration : IEntityTypeConfiguration<SchedulePlanEntity>
    {
        public void Configure(EntityTypeBuilder<SchedulePlanEntity> builder)
        {
            builder.ToTable("SchedulePlans");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.PlanId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PlanJson)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(1000);

            // 索引
            builder.HasIndex(x => x.PlanId).IsUnique();
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);

            // 复合索引 - 优化常见查询模式
            builder.HasIndex(x => new { x.Status, x.CreatedAt })
                .HasDatabaseName("IX_SchedulePlans_Status_CreatedAt");
        }
    }

    /// <summary>
    /// 执行计划实体配置
    /// </summary>
    public class ExecutionPlanConfiguration : IEntityTypeConfiguration<ExecutionPlanEntity>
    {
        public void Configure(EntityTypeBuilder<ExecutionPlanEntity> builder)
        {
            builder.ToTable("ExecutionPlans");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.PlanId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PlanJson)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(1000);

            // 索引
            builder.HasIndex(x => x.PlanId).IsUnique();
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);

            // 复合索引 - 优化常见查询模式
            builder.HasIndex(x => new { x.Status, x.CreatedAt })
                .HasDatabaseName(PersistenceConstants.IndexNames.ExecutionPlans_Status_CreatedAt);

            builder.HasIndex(x => new { x.PlanId, x.Status })
                .HasDatabaseName(PersistenceConstants.IndexNames.ExecutionPlans_PlanId_Status);
        }
    }

    /// <summary>
    /// 任务执行结果实体配置
    /// </summary>
    public class TaskExecutionResultConfiguration : IEntityTypeConfiguration<TaskExecutionResultEntity>
    {
        public void Configure(EntityTypeBuilder<TaskExecutionResultEntity> builder)
        {
            builder.ToTable("TaskExecutionResults");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.TaskId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PlanId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Message)
                .HasMaxLength(4000);

            builder.Property(x => x.Error)
                .HasMaxLength(4000);

            builder.Property(x => x.DataJson)
                .HasMaxLength(8000);

            // 索引
            builder.HasIndex(x => x.TaskId);
            builder.HasIndex(x => x.PlanId);
            builder.HasIndex(x => x.Success);
            builder.HasIndex(x => x.CreatedAt);

            // 复合索引 - 优化常见查询模式
            builder.HasIndex(x => new { x.PlanId, x.Success })
                .HasDatabaseName(PersistenceConstants.IndexNames.TaskExecutionResults_PlanId_Success);

            builder.HasIndex(x => new { x.PlanId, x.CreatedAt })
                .HasDatabaseName(PersistenceConstants.IndexNames.TaskExecutionResults_PlanId_CreatedAt);
        }
    }
}
