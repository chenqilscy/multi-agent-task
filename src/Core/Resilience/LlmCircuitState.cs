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
