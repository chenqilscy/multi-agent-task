using CKY.MultiAgentFramework.Core.Models.Resilience;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// LLM 熔断器（完整实现）
    /// 提供状态管理、失败阈值跟踪、自动恢复
    /// </summary>
    public class LlmCircuitBreaker
    {
        private readonly ILogger _logger;
        private readonly LlmCircuitBreakerOptions _options;
        private readonly object _lock = new();

        private LlmCircuitState _state = LlmCircuitState.Closed;
        private int _failureCount;
        private int _successCount;
        private DateTime? _lastStateChangeTime;
        private DateTime? _lastFailureTime;

        /// <summary>
        /// 当前熔断器状态
        /// </summary>
        public LlmCircuitState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// 当前失败计数
        /// </summary>
        public int FailureCount
        {
            get
            {
                lock (_lock)
                {
                    return _failureCount;
                }
            }
        }

        /// <summary>
        /// 最后一次状态变更时间
        /// </summary>
        public DateTime? LastStateChangeTime
        {
            get
            {
                lock (_lock)
                {
                    return _lastStateChangeTime;
                }
            }
        }

        public LlmCircuitBreaker(
            ILogger logger,
            LlmCircuitBreakerOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new LlmCircuitBreakerOptions();
            _lastStateChangeTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 执行操作（带熔断保护）
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            string agentId,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            // 检查是否允许执行
            if (!CanExecute(agentId))
            {
                _logger?.LogWarning("[CircuitBreaker] Circuit is OPEN for {AgentId}, rejecting request", agentId);
                throw new LlmCircuitBreakerOpenException(
                    $"Circuit breaker is OPEN for {agentId}. " +
                    $"Last state change: {_lastStateChangeTime:yyyy-MM-dd HH:mm:ss} UTC. " +
                    $"Try again later.");
            }

            try
            {
                // 执行操作
                var result = await operation(ct);

                // 记录成功
                RecordSuccess(agentId);

                return result;
            }
            catch (Exception ex)
            {
                // 记录失败
                RecordFailure(agentId, ex);
                throw;
            }
        }

        /// <summary>
        /// 检查是否允许执行操作
        /// </summary>
        private bool CanExecute(string agentId)
        {
            lock (_lock)
            {
                return _state switch
                {
                    LlmCircuitState.Closed => true,
                    LlmCircuitState.Open => ShouldAttemptReset(agentId),
                    LlmCircuitState.HalfOpen => true, // 半开状态允许测试请求
                    _ => false
                };
            }
        }

        /// <summary>
        /// 判断是否应该尝试重置熔断器
        /// </summary>
        private bool ShouldAttemptReset(string agentId)
        {
            if (!_lastStateChangeTime.HasValue)
                return false;

            var timeSinceStateChanged = DateTime.UtcNow - _lastStateChangeTime.Value;

            // 从 Open 转换到 HalfOpen
            if (_state == LlmCircuitState.Open &&
                timeSinceStateChanged >= _options.BreakDuration)
            {
                _logger?.LogInformation(
                    "[CircuitBreaker] Transitioning from OPEN to HALF_OPEN for {AgentId} after {Duration}s",
                    agentId, timeSinceStateChanged.TotalSeconds);

                _state = LlmCircuitState.HalfOpen;
                _lastStateChangeTime = DateTime.UtcNow;
                _successCount = 0; // 重置成功计数

                return true;
            }

            return false;
        }

        /// <summary>
        /// 记录成功操作
        /// </summary>
        private void RecordSuccess(string agentId)
        {
            lock (_lock)
            {
                _logger?.LogDebug(
                    "[CircuitBreaker] Recording success for {AgentId}, State: {State}, SuccessCount: {SuccessCount}",
                    agentId, _state, _successCount);

                if (_state == LlmCircuitState.HalfOpen)
                {
                    _successCount++;

                    // 半开状态下的成功请求，回到 Closed 状态
                    if (_successCount >= 1) // 成功一次即可恢复
                    {
                        _logger?.LogInformation(
                            "[CircuitBreaker] Transitioning from HALF_OPEN to CLOSED for {AgentId} after {SuccessCount} success(es)",
                            agentId, _successCount);

                        _state = LlmCircuitState.Closed;
                        _failureCount = 0;
                        _successCount = 0;
                        _lastStateChangeTime = DateTime.UtcNow;
                        _lastFailureTime = null;
                    }
                }
                else if (_state == LlmCircuitState.Closed)
                {
                    // 正常状态下，重置失败计数
                    _failureCount = 0;
                }
            }
        }

        /// <summary>
        /// 记录失败操作
        /// </summary>
        private void RecordFailure(string agentId, Exception ex)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                _logger?.LogWarning(
                    ex,
                    "[CircuitBreaker] Recording failure for {AgentId}, State: {State}, FailureCount: {FailureCount}/{Threshold}",
                    agentId, _state, _failureCount, _options.FailureThreshold);

                if (_state == LlmCircuitState.HalfOpen)
                {
                    // 半开状态下失败，立即回到 Open 状态
                    _logger?.LogError(
                        "[CircuitBreaker] Transitioning from HALF_OPEN back to OPEN for {AgentId} after test failure",
                        agentId);

                    _state = LlmCircuitState.Open;
                    _successCount = 0;
                    _lastStateChangeTime = DateTime.UtcNow;
                }
                else if (_state == LlmCircuitState.Closed)
                {
                    // 正常状态下，达到失败阈值则熔断
                    if (_failureCount >= _options.FailureThreshold)
                    {
                        _logger?.LogError(
                            "[CircuitBreaker] Transitioning from CLOSED to OPEN for {AgentId} after {FailureCount} failures (threshold: {Threshold})",
                            agentId, _failureCount, _options.FailureThreshold);

                        _state = LlmCircuitState.Open;
                        _lastStateChangeTime = DateTime.UtcNow;
                    }
                }
            }
        }

        /// <summary>
        /// 手动重置熔断器到 Closed 状态
        /// </summary>
        public void Reset(string agentId)
        {
            lock (_lock)
            {
                var previousState = _state;
                _state = LlmCircuitState.Closed;
                _failureCount = 0;
                _successCount = 0;
                _lastStateChangeTime = DateTime.UtcNow;
                _lastFailureTime = null;

                _logger?.LogInformation(
                    "[CircuitBreaker] Manually reset for {AgentId} from {PreviousState} to CLOSED",
                    agentId, previousState);
            }
        }

        /// <summary>
        /// 获取熔断器状态信息（用于监控）
        /// </summary>
        public LlmCircuitBreakerStatus GetStatus()
        {
            lock (_lock)
            {
                return new LlmCircuitBreakerStatus
                {
                    State = _state,
                    FailureCount = _failureCount,
                    SuccessCount = _successCount,
                    LastStateChangeTime = _lastStateChangeTime,
                    LastFailureTime = _lastFailureTime,
                    FailureThreshold = _options.FailureThreshold,
                    BreakDuration = _options.BreakDuration
                };
            }
        }
    }
}
