namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 资源需求（定义任务执行所需的资源）
    /// </summary>
    /// <remarks>
    /// 设计目的：
    /// - 评估任务是否可以立即执行（资源是否充足）
    /// - 防止资源耗尽（控制并发任务数量）
    /// - 支持资源隔离（独占访问）
    /// - 优化资源分配（按优先级调度）
    ///
    /// 主要属性：
    /// - RequiredAgentSlots: 需要的 Agent 槽位数（默认 1）
    /// - MaxExecutionTime: 最大执行时间（防止任务无限期运行）
    /// - RequiresExclusiveAccess: 是否需要独占访问（防止并发冲突）
    /// - RequiredCapabilities: 需要的能力列表（技能匹配）
    ///
    /// 使用场景：
    /// <code>
    /// // 高优先级任务，需要独占访问
    /// var highPriorityRequirements = new ResourceRequirements
    /// {
    ///     RequiredAgentSlots = 2,
    ///     MaxExecutionTime = TimeSpan.FromMinutes(10),
    ///     RequiresExclusiveAccess = true,
    ///     RequiredCapabilities = new List<string> { "CodeGeneration", "Testing" }
    /// };
    ///
    /// // 普通任务，共享资源
    /// var normalRequirements = new ResourceRequirements
    /// {
    ///     RequiredAgentSlots = 1,
    ///     MaxExecutionTime = TimeSpan.FromMinutes(5),
    ///     RequiresExclusiveAccess = false,
    ///     RequiredCapabilities = new List<string> { "General" }
    /// };
    /// </code>
    ///
    /// 资源调度策略：
    /// - 优先调度低资源需求的任务
    /// - 等待资源释放后再调度高资源需求任务
    /// - 独占访问任务暂停其他同类型任务
    /// - 超时任务自动终止并标记为失败
    /// </remarks>
    public class ResourceRequirements
    {
        /// <summary>
        /// 获取或设置需要的 Agent 槽位数
        /// </summary>
        /// <value>
        /// 默认值为 1，表示需要一个 Agent 来执行此任务。
        /// 值越大，占用的资源越多，调度优先级越低。
        /// </value>
        public int RequiredAgentSlots { get; set; } = 1;

        /// <summary>
        /// 获取或设置最大执行时间
        /// </summary>
        /// <value>
        /// 默认值为 5 分钟。超过此时间任务将被强制终止。
        /// 用于防止任务无限期运行或死锁。
        /// </value>
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 获取或设置是否需要独占访问
        /// </summary>
        /// <value>
        /// 如果为 true，则在任务执行期间，其他需要相同资源的任务将被阻塞。
        /// 用于防止并发冲突（如写入同一文件、修改同一配置）。
        /// </value>
        public bool RequiresExclusiveAccess { get; set; }

        /// <summary>
        /// 获取或设置需要的能力列表
        /// </summary>
        /// <value>
        /// 定义执行此任务所需的 Agent 能力（如 "CodeGeneration", "Testing", "DataAnalysis"）。
        /// 任务调度器会根据此列表匹配合适的 Agent。
        /// </value>
        public List<string> RequiredCapabilities { get; set; } = new();
    }
}
