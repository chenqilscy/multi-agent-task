namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// LLM 弹性管道异常（重试策略失败后抛出）
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 重试策略耗尽后仍失败
    /// - 熔断器开启后调用被拒绝
    /// - 超时策略触发后仍未完成
    /// - 舱舱隔离失败（资源耗尽）
    ///
    /// 主要特点：
    /// - IsRetryable = false: 已经过过重试，不应再次重试
    /// - 包含内部异常: 保留原始异常信息
    /// - 标识弹性管道失败：区别于直接 LLM 调用失败
    ///
    /// 弹性策略：
    /// - 重试（Retry）: 指数退避重试
    /// - 熔断（Circuit Breaker）: 连续失败后快速失败
    /// - 超时（Timeout）: 设置最大执行时间
    /// - 舱舱（Bulkhead）: 限制并发请求数
    ///
    /// 错误处理建议：
    /// - 不应再次重试（已经过重试）
    /// - 触发降级策略（使用备用模型、规则引擎）
    /// - 记录详细的错误链用于诊断
    /// - 考虑告警通知（连续失败可能表示服务异常）
    ///
    /// 示例：
    /// <code>
    /// try
    /// {
    ///     var response = await _resilientLlmClient.InvokeAsync(prompt);
    /// }
    /// catch (LlmResilienceException ex)
    /// {
    ///     // 弹性策略失败，触发降级
    ///     _logger.LogError(ex, "LLM 弹性管道失败，触发降级策略");
    ///     return await _ruleEngine.ProcessAsync(prompt);
    /// }
    /// </code>
    /// </remarks>
    public class LlmResilienceException : MafException
    {
        /// <summary>
        /// 获取导致此异常的内部异常
        /// </summary>
        public new Exception? InnerException { get; }

        /// <summary>
        /// 初始化 LlmResilienceException 类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public LlmResilienceException(
            string message,
            Exception? innerException = null)
            : base(MafErrorCode.LlmServiceError, message, isRetryable: false, component: "LlmResiliencePipeline")
        {
            InnerException = innerException;
        }
    }
}
