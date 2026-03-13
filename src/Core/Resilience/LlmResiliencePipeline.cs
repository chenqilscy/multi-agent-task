using CKY.MultiAgentFramework.Core.Models.Resilience;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// LLM 调用弹性管道（整合重试、熔断、超时）
    /// 注意：这是一个简化实现，完整的实现在 Services 层
    /// </summary>
    public class LlmResiliencePipeline : ILlmResiliencePipeline
    {
        private readonly ILogger _logger;
        private readonly LlmCircuitBreakerOptions _options;

        public LlmResiliencePipeline(
            ILogger logger,
            LlmCircuitBreakerOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new LlmCircuitBreakerOptions();
        }

        /// <summary>
        /// 执行带弹性保护的 LLM 调用
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            string agentId,
            Func<CancellationToken, Task<T>> operation,
            TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);

            _logger?.LogDebug("[LlmResilience] Executing operation for {AgentId} with timeout {Timeout}s",
                agentId, actualTimeout.TotalSeconds);

            // 使用 CancellationSource 实现超时
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(actualTimeout);

            try
            {
                // 简单的重试逻辑（最多 3 次）
                int attempt = 0;
                Exception? lastException = null;

                while (attempt <= 3)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            _logger.LogInformation("Retry attempt {Attempt}/3", attempt);
                            // 指数退避
                            await Task.Delay(1000 * (int)Math.Pow(2, attempt - 1), ct);
                        }

                        return await operation(cts.Token);
                    }
                    catch (Exception ex) when (attempt < 3 && IsRetryable(ex))
                    {
                        lastException = ex;
                        attempt++;
                        if (_logger != null)
                        {
                            _logger.LogWarning(ex, "[LlmResilience] Operation failed for {AgentId} (attempt {Attempt}), retrying...",
                                agentId, attempt - 1);
                        }
                    }
                }

                throw new InvalidOperationException(
                    $"Operation failed after 4 attempts for {agentId}",
                    lastException);
            }
            catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                _logger?.LogWarning("[LlmResilience] Operation timed out for {AgentId} after {Timeout}s",
                    agentId, actualTimeout.TotalSeconds);
                throw new TimeoutException($"LLM call to {agentId} timed out after {actualTimeout.TotalSeconds}s", ex);
            }
        }

        private static bool IsRetryable(Exception ex)
        {
            return ex switch
            {
                TaskCanceledException => false,
                OperationCanceledException => false,
                ArgumentNullException => false,
                ArgumentException => false,
                _ => true
            };
        }
    }
}
