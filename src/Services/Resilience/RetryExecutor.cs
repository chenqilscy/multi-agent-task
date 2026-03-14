using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Resilience
{
    /// <summary>
    /// 退避策略
    /// </summary>
    public enum BackoffStrategy
    {
        Fixed,
        Linear,
        Exponential,
        ExponentialWithJitter
    }

    /// <summary>
    /// 重试策略配置
    /// </summary>
    public class RetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.ExponentialWithJitter;
        public int InitialBackoffMs { get; set; } = 1000;
        public int MaxBackoffMs { get; set; } = 30000;
        public double JitterFactor { get; set; } = 0.1;
    }

    /// <summary>
    /// 重试执行器
    /// </summary>
    public class RetryExecutor
    {
        private readonly ILogger<RetryExecutor> _logger;
        private readonly Random _random = new();

        public RetryExecutor(ILogger<RetryExecutor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行带重试的操作
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            RetryPolicy policy,
            CancellationToken ct = default)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= policy.MaxRetries)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retry attempt {Attempt}/{MaxRetries}",
                            attempt, policy.MaxRetries);
                    }

                    return await operation(ct);
                }
                catch (Exception ex) when (attempt < policy.MaxRetries && IsRetryable(ex))
                {
                    lastException = ex;
                    attempt++;

                    var delay = CalculateBackoff(attempt, policy);
                    _logger.LogWarning(ex,
                        "Operation failed (attempt {Attempt}). Retrying in {Delay}ms",
                        attempt - 1, delay);

                    await Task.Delay(delay, ct);
                }
            }

            throw new InvalidOperationException(
                $"Operation failed after {policy.MaxRetries + 1} attempts",
                lastException);
        }

        private int CalculateBackoff(int attempt, RetryPolicy policy)
        {
            int baseDelay = policy.BackoffStrategy switch
            {
                BackoffStrategy.Fixed => policy.InitialBackoffMs,
                BackoffStrategy.Linear => policy.InitialBackoffMs * attempt,
                BackoffStrategy.Exponential => policy.InitialBackoffMs * (int)Math.Pow(2, attempt - 1),
                BackoffStrategy.ExponentialWithJitter => policy.InitialBackoffMs * (int)Math.Pow(2, attempt - 1),
                _ => policy.InitialBackoffMs
            };

            // 应用最大值限制
            baseDelay = Math.Min(baseDelay, policy.MaxBackoffMs);

            // 应用抖动
            if (policy.BackoffStrategy == BackoffStrategy.ExponentialWithJitter)
            {
                var jitter = (int)(baseDelay * policy.JitterFactor * (_random.NextDouble() * 2 - 1));
                baseDelay += jitter;
            }

            return Math.Max(0, baseDelay);
        }

        private static bool IsRetryable(Exception ex)
        {
            return ex switch
            {
                TaskCanceledException => false,
                OperationCanceledException => false,
                ArgumentNullException => false,  // More specific, must come before ArgumentException
                ArgumentException => false,
                _ => true
            };
        }
    }
}
