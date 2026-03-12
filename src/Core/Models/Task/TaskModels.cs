using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 任务依赖关系
    /// </summary>
    public class TaskDependency
    {
        /// <summary>依赖的任务ID</summary>
        public string DependsOnTaskId { get; set; } = string.Empty;

        /// <summary>依赖类型</summary>
        public DependencyType Type { get; set; }

        /// <summary>依赖是否已满足</summary>
        public bool IsSatisfied { get; set; }

        /// <summary>可选条件表达式</summary>
        public string? Condition { get; set; }

        /// <summary>
        /// 检查依赖是否满足
        /// </summary>
        public bool CheckSatisfied(DecomposedTask targetTask)
        {
            if (targetTask == null) return false;

            return Type switch
            {
                DependencyType.MustComplete => targetTask.Status == MafTaskStatus.Completed,
                DependencyType.MustSucceed => targetTask.Status == MafTaskStatus.Completed
                                      && targetTask.Result?.Success == true,
                DependencyType.MustStart => targetTask.Status == MafTaskStatus.Running
                                      || targetTask.Status == MafTaskStatus.Completed,
                DependencyType.DataDependency => targetTask.Status == MafTaskStatus.Completed
                                           && targetTask.Result?.Data != null,
                _ => false
            };
        }
    }

    /// <summary>
    /// 资源需求
    /// </summary>
    public class ResourceRequirements
    {
        /// <summary>需要的Agent槽位数</summary>
        public int RequiredAgentSlots { get; set; } = 1;

        /// <summary>最大执行时间</summary>
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>是否需要独占访问</summary>
        public bool RequiresExclusiveAccess { get; set; }

        /// <summary>需要的能力列表</summary>
        public List<string> RequiredCapabilities { get; set; } = new();
    }

    /// <summary>
    /// 分解后的子任务
    /// </summary>
    public class DecomposedTask
    {
        /// <summary>任务唯一标识</summary>
        public string TaskId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>任务名称</summary>
        public string TaskName { get; set; } = string.Empty;

        /// <summary>任务意图</summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>任务描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>任务优先级</summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>优先级评分 (0-100，数值越高优先级越高)</summary>
        public int PriorityScore { get; set; }

        /// <summary>优先级原因</summary>
        public PriorityReason PriorityReason { get; set; }

        /// <summary>任务依赖关系列表</summary>
        public List<TaskDependency> Dependencies { get; set; } = new();

        /// <summary>任务是否被阻塞（有未满足的依赖）</summary>
        public bool IsBlocked => Dependencies.Any(d => !d.IsSatisfied);

        /// <summary>执行策略</summary>
        public ExecutionStrategy ExecutionStrategy { get; set; } = ExecutionStrategy.Immediate;

        /// <summary>预估执行时长</summary>
        public TimeSpan EstimatedDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>最大等待时间</summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>需要的能力</summary>
        public string RequiredCapability { get; set; } = string.Empty;

        /// <summary>目标Agent ID</summary>
        public string? TargetAgentId { get; set; }

        /// <summary>资源需求</summary>
        public ResourceRequirements ResourceRequirements { get; set; } = new();

        /// <summary>任务参数</summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>任务上下文</summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>任务状态</summary>
        public MafTaskStatus Status { get; set; } = MafTaskStatus.Pending;

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>开始时间</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>完成时间</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>执行结果</summary>
        public TaskExecutionResult? Result { get; set; }
    }

    /// <summary>
    /// 任务请求
    /// </summary>
    public class MafTaskRequest
    {
        /// <summary>任务唯一标识</summary>
        public string TaskId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>用户原始输入</summary>
        public string UserInput { get; set; } = string.Empty;

        /// <summary>对话ID</summary>
        public string ConversationId { get; set; } = string.Empty;

        /// <summary>用户ID</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>显式参数</summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>隐式上下文</summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>任务优先级</summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    }

    /// <summary>
    /// 子任务执行结果
    /// </summary>
    public class SubTaskResult
    {
        /// <summary>子任务ID</summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>输出消息</summary>
        public string? Message { get; set; }

        /// <summary>错误信息</summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// 任务响应
    /// </summary>
    public class MafTaskResponse
    {
        /// <summary>任务ID</summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>结果消息</summary>
        public string? Result { get; set; }

        /// <summary>结果数据</summary>
        public object? Data { get; set; }

        /// <summary>错误信息</summary>
        public string? Error { get; set; }

        /// <summary>子任务结果列表</summary>
        public List<SubTaskResult> SubTaskResults { get; set; } = new();

        /// <summary>完成时间</summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 任务执行结果
    /// </summary>
    public class TaskExecutionResult
    {
        /// <summary>任务ID</summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>输出消息</summary>
        public string? Message { get; set; }

        /// <summary>结果数据</summary>
        public object? Data { get; set; }

        /// <summary>错误信息</summary>
        public string? Error { get; set; }

        /// <summary>开始时间</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>完成时间</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>执行时长</summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>重试次数</summary>
        public int RetryCount { get; set; }
    }
}
