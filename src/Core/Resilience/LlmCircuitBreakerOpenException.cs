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
