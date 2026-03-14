namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 子任务执行结果（子任务的简化结果模型）
    /// </summary>
    /// <remarks>
    /// 设计目的：
    /// - 提供子任务的轻量级结果模型
    /// - 区分成功和失败的结果
    /// - 包含错误信息用于诊断
    /// - 支持子任务的组合和聚合
    ///
    /// 与 TaskExecutionResult 的区别：
    /// - SubTaskResult: 简化模型，用于子任务
    /// - TaskExecutionResult: 完整模型，用于主任务
    ///
    /// 主要属性：
    /// - Success: 是否成功执行
    /// - Message: 输出消息（成功时的描述）
    /// - Error: 错误信息（失败时的错误详情）
    ///
    /// 使用场景：
    /// <code>
    /// // 成功的子任务结果
    /// var successResult = new SubTaskResult
    /// {
    ///     TaskId = "task-123",
    ///     Success = true,
    ///     Message = "成功生成了用户认证 API 代码"
    /// };
    ///
    /// // 失败的子任务结果
    /// var failureResult = new SubTaskResult
    /// {
    ///     TaskId = "task-456",
    ///     Success = false,
    ///     Error = "LLM API 调用失败：网络超时"
    /// };
    /// </code>
    ///
    /// 结果聚合：
    /// 主任务可以聚合所有子任务的结果，判断整体是否成功。
    /// </remarks>
    public class SubTaskResult
    {
        /// <summary>
        /// 获取或设置子任务 ID
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
        /// 获取或设置错误信息（失败时的错误详情）
        /// </summary>
        public string? Error { get; set; }
    }
}
