namespace CKY.MultiAgentFramework.Core.Models.Resilience
{
    /// <summary>
    /// LLM 熔断器配置选项
    /// </summary>
    public class LlmCircuitBreakerOptions
    {
        /// <summary>
        /// 失败阈值：连续失败多少次后熔断（默认 3 次）
        /// </summary>
        public int FailureThreshold { get; set; } = 3;

        /// <summary>
        /// 熔断持续时间（默认 5 分钟）
        /// </summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 半开状态的测试请求超时（默认 30 秒）
        /// </summary>
        public TimeSpan HalfOpenTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
