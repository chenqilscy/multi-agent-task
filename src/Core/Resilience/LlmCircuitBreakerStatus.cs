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
