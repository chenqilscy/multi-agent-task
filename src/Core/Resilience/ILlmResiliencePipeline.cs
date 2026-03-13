namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// LLM 弹性管道接口
    /// </summary>
    public interface ILlmResiliencePipeline
    {
        /// <summary>
        /// 执行带弹性保护的 LLM 调用
        /// </summary>
        Task<T> ExecuteAsync<T>(
            string agentId,
            Func<CancellationToken, Task<T>> operation,
            TimeSpan? timeout = null,
            CancellationToken ct = default);
    }
}
