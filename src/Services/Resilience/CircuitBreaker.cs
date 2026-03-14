using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Resilience
{
    /// <summary>
    /// 熔断器状态
    /// </summary>
    public enum CircuitState
    {
        Closed,     // 正常状态
        Open,       // 熔断状态
        HalfOpen    // 半开状态（尝试恢复）
    }

    /// <summary>
    /// 熔断器配置
    /// </summary>
    public class CircuitBreakerConfig
    {
        public int FailureThreshold { get; set; } = 5;
        public TimeSpan SuccessThreshold { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan HalfOpenTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// 熔断器
    /// </summary>
    public class CircuitBreaker
    {
        private readonly ILogger<CircuitBreaker> _logger;
        private readonly CircuitBreakerConfig _config;
        private readonly object _lock = new();

        private CircuitState _state = CircuitState.Closed;
        private int _failureCount;
        private DateTime? _lastFailureTime;
        private DateTime? _stateChangedTime;

        public CircuitBreaker(
            CircuitBreakerConfig config,
            ILogger<CircuitBreaker> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行操作（带熔断保护）
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            string componentName,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            if (!CanExecute(componentName))
            {
                throw new CircuitBreakerOpenException(
                    $"Circuit breaker is OPEN for {componentName}. Try again later.");
            }

            try
            {
                var result = await operation(ct);
                RecordSuccess();
                return result;
            }
            catch
            {
                RecordFailure();
                throw;
            }
        }

        private bool CanExecute(string componentName)
        {
            lock (_lock)
            {
                return _state switch
                {
                    CircuitState.Closed => true,
                    CircuitState.Open => ShouldAttemptReset(),
                    CircuitState.HalfOpen => ShouldAttemptReset(),
                    _ => false
                };
            }
        }

        private bool ShouldAttemptReset()
        {
            if (!_stateChangedTime.HasValue) return false;

            var timeSinceStateChanged = DateTime.UtcNow - _stateChangedTime.Value;

            if (_state == CircuitState.Open &&
                timeSinceStateChanged >= _config.OpenTimeout)
            {
                _logger.LogInformation("Circuit breaker transitioning from OPEN to HALF_OPEN");
                _state = CircuitState.HalfOpen;
                _stateChangedTime = DateTime.UtcNow;
                return true;
            }

            if (_state == CircuitState.HalfOpen &&
                timeSinceStateChanged >= _config.HalfOpenTimeout)
            {
                _logger.LogInformation("Circuit breaker transitioning from HALF_OPEN back to OPEN");
                _state = CircuitState.Open;
                _stateChangedTime = DateTime.UtcNow;
                return false;
            }

            return false;
        }

        private void RecordSuccess()
        {
            lock (_lock)
            {
                if (_state == CircuitState.HalfOpen)
                {
                    _logger.LogInformation("Circuit breaker transitioning from HALF_OPEN to CLOSED");
                    _state = CircuitState.Closed;
                    _failureCount = 0;
                    _lastFailureTime = null;
                    _stateChangedTime = DateTime.UtcNow;
                }
                else if (_state == CircuitState.Closed)
                {
                    _failureCount = 0;
                }
            }
        }

        private void RecordFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_state == CircuitState.Closed &&
                    _failureCount >= _config.FailureThreshold)
                {
                    _logger.LogWarning("Circuit breaker transitioning from CLOSED to OPEN after {Failures} failures",
                        _failureCount);
                    _state = CircuitState.Open;
                    _stateChangedTime = DateTime.UtcNow;
                }
            }
        }

        public CircuitState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }
    }

    /// <summary>
    /// 熔断器开启异常
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}
