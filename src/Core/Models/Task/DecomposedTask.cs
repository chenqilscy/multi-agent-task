using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 分解后的子任务（任务分解的原子执行单元）
    /// </summary>
    /// <remarks>
    /// 设计目的：
    /// - 将复杂的用户请求分解为可执行的小任务
    /// - 支持任务的串行和并行执行
    /// - 提供任务调度的完整信息（优先级、依赖、资源）
    /// - 跟踪任务执行的完整生命周期
    ///
    /// 核心概念：
    /// - 原子性: 每个任务是一个不可分割的执行单元
    /// - 可调度: 包含调度所需的所有信息（优先级、依赖、资源）
    /// - 可跟踪: 记录任务的状态变化和执行结果
    /// - 可组合: 多个子任务可组合为复杂的执行流程
    ///
    /// 任务生命周期：
    /// 1. Pending（待执行）: 已创建，等待调度
    /// 2. Running（执行中）: 已分配 Agent，正在执行
    /// 3. Completed（已完成）: 执行完成（成功或失败）
    /// 4. Failed（失败）: 执行失败，可重试
    /// 5. Cancelled（已取消）: 被取消或超时
    ///
    /// 使用场景：
    /// <code>
    /// // 创建代码生成子任务
    /// var codeGenTask = new DecomposedTask
    /// {
    ///     TaskName = "生成用户认证 API",
    ///     Intent = "CodeGeneration",
    ///     Description = "生成基于 JWT 的用户认证 API 代码",
    ///     Priority = TaskPriority.High,
    ///     PriorityScore = 85,
    ///     PriorityReason = PriorityReason.UserRequested,
    ///     ExecutionStrategy = ExecutionStrategy.Immediate,
    ///     RequiredCapability = "CodeGeneration",
    ///     EstimatedDuration = TimeSpan.FromMinutes(3),
    ///     Parameters = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["Language"] = "C#",
    ///         ["Framework"] = "ASP.NET Core",
    ///         ["AuthenticationType"] = "JWT"
    ///     }
    /// };
    /// </code>
    ///
    /// 任务依赖示例：
    /// <code>
    /// // 测试任务依赖代码生成任务
    /// testTask.Dependencies.Add(new TaskDependency
    /// {
    ///     DependsOnTaskId = codeGenTask.TaskId,
    ///     Type = DependencyType.MustSucceed
    /// });
    /// </code>
    /// </remarks>
    public class DecomposedTask
    {
        /// <summary>
        /// 获取或设置任务唯一标识
        /// </summary>
        public string TaskId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 获取或设置任务名称
        /// </summary>
        public string TaskName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置任务意图（用于 Agent 匹配）
        /// </summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置任务描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置任务优先级
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>
        /// 获取或设置优先级评分（0-100，数值越高优先级越高）
        /// </summary>
        public int PriorityScore { get; set; }

        /// <summary>
        /// 获取或设置优先级原因
        /// </summary>
        public PriorityReason PriorityReason { get; set; }

        /// <summary>
        /// 获取或设置任务依赖关系列表
        /// </summary>
        public List<TaskDependency> Dependencies { get; set; } = new();

        /// <summary>
        /// 获取一个值，指示任务是否被阻塞（有未满足的依赖）
        /// </summary>
        public bool IsBlocked => Dependencies.Any(d => !d.IsSatisfied);

        /// <summary>
        /// 获取或设置执行策略
        /// </summary>
        public ExecutionStrategy ExecutionStrategy { get; set; } = ExecutionStrategy.Immediate;

        /// <summary>
        /// 获取或设置预估执行时长
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 获取或设置最大等待时间
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 获取或设置需要的能力
        /// </summary>
        public string RequiredCapability { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置目标 Agent ID
        /// </summary>
        public string? TargetAgentId { get; set; }

        /// <summary>
        /// 获取或设置资源需求
        /// </summary>
        public ResourceRequirements ResourceRequirements { get; set; } = new();

        /// <summary>
        /// 获取或设置任务参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 获取或设置任务上下文
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// 获取或设置任务状态
        /// </summary>
        public MafTaskStatus Status { get; set; } = MafTaskStatus.Pending;

        /// <summary>
        /// 获取或设置创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 获取或设置开始时间
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 获取或设置完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 获取或设置执行结果
        /// </summary>
        public TaskExecutionResult? Result { get; set; }
    }
}
