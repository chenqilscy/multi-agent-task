namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 任务执行结果（完整的任务执行结果模型）
    /// </summary>
    /// <remarks>
    /// 设计目的：
    /// - 提供任务执行的完整结果信息
    /// - 支持成功和失败结果的统一表示
    /// - 包含执行过程的元数据（时长、重试次数）
    /// - 支持结构化的结果数据
    ///
    /// 主要属性：
    /// - Success: 是否成功执行
    /// - Message: 输出消息（成功时的描述）
    /// - Data: 结果数据（结构化的输出数据）
    /// - Error: 错误信息（失败时的错误详情）
    /// - Duration: 执行时长
    /// - RetryCount: 重试次数
    ///
    /// 使用场景：
    /// <code>
    /// // 成功的任务执行结果
    /// var successResult = new TaskExecutionResult
    /// {
    ///     TaskId = "task-123",
    ///     Success = true,
    ///     Message = "成功生成了用户认证 API",
    ///     Data = new
    ///     {
    ///         Code = "生成的代码内容",
    ///         File = "AuthController.cs",
    ///         Lines = 150
    ///     },
    ///     StartedAt = DateTime.UtcNow.AddMinutes(-5),
    ///     CompletedAt = DateTime.UtcNow,
    ///     Duration = TimeSpan.FromMinutes(5),
    ///     RetryCount = 0
    /// };
    ///
    /// // 失败的任务执行结果
    /// var failureResult = new TaskExecutionResult
    /// {
    ///     TaskId = "task-456",
    ///     Success = false,
    ///     Error = "LLM API 调用失败：网络超时",
    ///     StartedAt = DateTime.UtcNow.AddMinutes(-1),
    ///     CompletedAt = DateTime.UtcNow,
    ///     Duration = TimeSpan.FromMinutes(1),
    ///     RetryCount = 3
    /// };
    /// </code>
    ///
    /// 结果数据分析：
    /// - 成功时：Data 包含结构化的输出数据（代码、文档、分析结果等）
    /// - 失败时：Error 包含错误信息，Data 为 null
    ///
    /// 性能分析：
    /// - Duration: 分析任务执行时长，优化性能
    /// - RetryCount: 分析重试次数，优化重试策略
    /// </remarks>
    public class TaskExecutionResult
    {
        /// <summary>
        /// 获取或设置任务 ID
        /// </summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置是否成功执行
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 获取或设置输出消息（成功时的描述）
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 获取或设置结果数据（结构化的输出数据）
        /// </summary>
        /// <remarks>
        /// 示例：生成的代码、文档、分析结果等
        /// </remarks>
        public object? Data { get; set; }

        /// <summary>
        /// 获取或设置错误信息（失败时的错误详情）
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// 获取或设置开始时间
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// 获取或设置完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 获取或设置执行时长
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// 获取或设置重试次数
        /// </summary>
        public int RetryCount { get; set; }
    }
}
