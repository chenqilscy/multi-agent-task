using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Core.Models.Session;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Repository.Data.EntityTypeConfigurations;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Repository.Data
{
    /// <summary>
    /// MAF 数据库上下文
    /// </summary>
    public class MafDbContext : DbContext
    {
        public MafDbContext(DbContextOptions<MafDbContext> options)
            : base(options)
        {
        }

        // 任务实体
        public DbSet<MainTask> MainTasks => Set<MainTask>();
        public DbSet<SubTask> SubTasks => Set<SubTask>();

        // 调度和执行计划实体
        public DbSet<SchedulePlanEntity> SchedulePlans => Set<SchedulePlanEntity>();
        public DbSet<ExecutionPlanEntity> ExecutionPlans => Set<ExecutionPlanEntity>();
        public DbSet<TaskExecutionResultEntity> TaskExecutionResults => Set<TaskExecutionResultEntity>();

        // LLM 提供商配置
        public DbSet<LlmProviderConfigEntity> LlmProviderConfigs => Set<LlmProviderConfigEntity>();

        // 会话存储（统一模型）
        public DbSet<MafSessionState> Sessions => Set<MafSessionState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 应用配置
            modelBuilder.ApplyConfiguration(new MainTaskConfiguration());
            modelBuilder.ApplyConfiguration(new SubTaskConfiguration());

            // 应用新实体配置
            modelBuilder.ApplyConfiguration(new SchedulePlanConfiguration());
            modelBuilder.ApplyConfiguration(new ExecutionPlanConfiguration());
            modelBuilder.ApplyConfiguration(new TaskExecutionResultConfiguration());

            // 应用 LLM 配置
            modelBuilder.ApplyConfiguration(new LlmProviderConfigConfiguration());

            // 应用会话配置（统一模型）
            modelBuilder.ApplyConfiguration(new SessionConfiguration());
        }
    }
}
