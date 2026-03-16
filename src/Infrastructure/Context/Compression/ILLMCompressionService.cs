using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CKY.MultiAgentFramework.Infrastructure.Context.Compression
{
    /// <summary>
    /// LLM 压缩服务接口
    /// </summary>
    public interface ILLMCompressionService
    {
        /// <summary>
        /// 总结消息列表
        /// </summary>
        Task<string> SummarizeAsync(List<ChatMessage> messages, CancellationToken cancellationToken);
    }
}
