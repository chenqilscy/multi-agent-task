using CKY.MultiAgentFramework.Core.Exceptions;
using CKY.MultiAgentFramework.Core.Models.Resilience;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// LLM 调用弹性管道（整合重试、熔断、超时）
    /// 完整实现：包含指数退避重试、熔断器状态管理、超时保护
    /// </summary>
    public class LlmResiliencePipeline : ILlmResiliencePipeline
    {
        private readonly ILogger _logger;
        private readonly LlmCircuitBreakerOptions _options;
        private readonly ConcurrentDictionary<string, LlmCircuitBreaker> _circuitBreakers;

        public LlmResiliencePipeline(
            ILogger logger,
            LlmCircuitBreakerOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new LlmCircuitBreakerOptions();
            _circuitBreakers = new ConcurrentDictionary<string, LlmCircuitBreaker>();
        }

        /// <summary>
        /// 获取或创建指定 Agent 的熔断器
        /// </summary>
        private LlmCircuitBreaker GetCircuitBreaker(string agentId)
        {
            return _circuitBreakers.GetOrAdd(agentId,
                id => new LlmCircuitBreaker(_logger, _options));
        }

        /// <summary>
        /// 执行带弹性保护的 LLM 调用（完整实现）
        /// 包含：熔断器 → 超时保护 → 指数退避重试
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            string agentId,
            Func<CancellationToken, Task<T>> operation,
            TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var circuitBreaker = GetCircuitBreaker(agentId);

            _logger?.LogDebug(
                "[LlmResilience] Executing operation for {AgentId} with timeout {Timeout}s, CircuitState: {CircuitState}",
                agentId, actualTimeout.TotalSeconds, circuitBreaker.State);

            // 步骤 1: 熔断器检查（第一道防线）
            // 如果熔断器已打开，直接拒绝请求
            return await circuitBreaker.ExecuteAsync(
                agentId,
                async (innerCt) => await ExecuteWithRetryAsync(
                    agentId,
                    operation,
                    actualTimeout,
                    innerCt),
                ct);
        }

        /// <summary>
        /// 执行带超时和重试的操作（内部方法）
        /// </summary>
        private async Task<T> ExecuteWithRetryAsync<T>(
            string agentId,
            Func<CancellationToken, Task<T>> operation,
            TimeSpan timeout,
            CancellationToken ct)
        {
            // 步骤 2: 超时保护（第二道防线）
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            try
            {
                // 步骤 3: 指数退避重试（第三道防线）
                int attempt = 0;
                Exception? lastException = null;
                const int maxAttempts = 3;

                while (attempt <= maxAttempts)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            _logger?.LogInformation(
                                "[LlmResilience] Retry attempt {Attempt}/{MaxAttempts} for {AgentId}",
                                attempt, maxAttempts, agentId);

                            // 指数退避：1s, 2s, 4s
                            var delayMs = 1000 * (int)Math.Pow(2, attempt - 1);
                            await Task.Delay(delayMs, ct);
                        }

                        return await operation(cts.Token);
                    }
                    catch (Exception ex) when (attempt < maxAttempts && IsRetryable(ex))
                    {
                        lastException = ex;
                        attempt++;

                        _logger?.LogWarning(
                            ex,
                            "[LlmResilience] Operation failed for {AgentId} (attempt {Attempt}/{MaxAttempts}), retrying...",
                            agentId, attempt, maxAttempts);
                    }
                }

                // 所有重试都失败
                throw new LlmResilienceException(
                    $"Operation failed after {maxAttempts + 1} attempts for {agentId}",
                    lastException);
            }
            catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // 超时异常
                _logger?.LogWarning(
                    "[LlmResilience] Operation timed out for {AgentId} after {Timeout}s",
                    agentId, timeout.TotalSeconds);

                throw new TimeoutException(
                    $"LLM call to {agentId} timed out after {timeout.TotalSeconds}s",
                    ex);
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
