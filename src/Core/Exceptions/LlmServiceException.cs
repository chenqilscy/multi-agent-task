namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// LLM 服务异常（LLM API 调用失败）
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - LLM API 调用失败（网络错误、超时）
    /// - LLM API 返回错误（认证失败、模型不可用）
    /// - LLM API 限流（请求过于频繁）
    /// - LLM 响应格式错误（无法解析）
    /// - LLM 配额不足（token 超限）
    ///
    /// 主要属性：
    /// - StatusCode: HTTP 状态码（如 401、429、500）
    /// - IsRateLimited: 是否被限流（可触发降级策略）
    ///
    /// 错误处理建议：
    /// - StatusCode = 401: 检查 API Key 配置
    /// - StatusCode = 429 或 IsRateLimited = true: 触发限流处理，延迟重试
    /// - StatusCode = 500: 服务器错误，可重试
    /// - IsRetryable = true: 启用指数退避重试
    ///
    /// 示例：
    /// <code>
    /// try
    /// {
    ///     var response = await _llmClient.InvokeAsync(prompt);
    /// }
    /// catch (LlmServiceException ex) when (ex.IsRateLimited)
    /// {
    ///     // 触发限流降级策略
    ///     await Task.Delay(TimeSpan.FromSeconds(60));
    /// }
    /// catch (LlmServiceException ex) when (ex.StatusCode == 401)
    /// {
    ///     // 认证失败，检查配置
    ///     _logger.LogError(ex, "LLM API 认证失败，请检查 API Key");
    /// }
    /// </code>
    /// </remarks>
    public class LlmServiceException : MafException
    {
        /// <summary>
        /// 获取 HTTP 状态码（如果有）
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// 获取一个值，指示是否被限流
        /// </summary>
        public bool IsRateLimited { get; }

        /// <summary>
        /// 初始化 LlmServiceException 类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="statusCode">HTTP 状态码（可选）</param>
        /// <param name="isRateLimited">是否被限流</param>
        public LlmServiceException(
            string message,
            int? statusCode = null,
            bool isRateLimited = false)
            : base(MafErrorCode.LlmServiceError, message, isRetryable: true, component: "LlmService")
        {
            StatusCode = statusCode;
            IsRateLimited = isRateLimited;
        }
    }
}
