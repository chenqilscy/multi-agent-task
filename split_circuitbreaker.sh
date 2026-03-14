#!/bin/bash
# 拆分 LlmCircuitBreaker.cs

mkdir -p src/Core/Resilience

# 创建独立文件
cat > src/Core/Resilience/LlmCircuitState.cs << 'STATEEOF'
namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// LLM 熔断器状态
    /// </summary>
    public enum LlmCircuitState
    {
        /// <summary>正常状态，允许请求通过</summary>
        Closed,

        /// <summary>熔断状态，拒绝请求</summary>
        Open,

        /// <summary>半开状态，允许测试请求通过</summary>
        HalfOpen
    }
}
STATEEOF

cat > src/Core/Resilience/LlmCircuitBreakerStatus.cs << 'STATUSEOF'
namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// LLM 熔断器状态信息（用于监控和诊断）
    /// </summary>
    public class LlmCircuitBreakerStatus
    {
        public LlmCircuitState State { get; set; }
        public int FailureCount { get; set; }
        public int SuccessCount { get; set; }
        public DateTime? LastStateChangeTime { get; set; }
        public DateTime? LastFailureTime { get; set; }
        public int FailureThreshold { get; set; }
        public TimeSpan BreakDuration { get; set; }

        public override string ToString()
        {
            return $"State: {State}, Failures: {FailureCount}/{FailureThreshold}, " +
                   $"LastStateChange: {LastStateChangeTime:yyyy-MM-dd HH:mm:ss} UTC";
        }
    }
}
STATUSEOF

cat > src/Core/Resilience/LlmCircuitBreakerOpenException.cs << 'EXCEOF'
namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// LLM 熔断器开启异常
    /// </summary>
    public class LlmCircuitBreakerOpenException : Exception
    {
        public LlmCircuitBreakerOpenException(string message) : base(message)
        {
        }

        public LlmCircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
EXCEOF

echo "✅ LlmCircuitBreaker.cs 拆分完成"
