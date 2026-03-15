using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Message;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 上下文压缩器接口
    /// 负责压缩对话历史以降低Token消耗
    /// Context compressor interface
    /// Responsible for compressing dialog history to reduce token consumption
    /// </summary>
    public interface IContextCompressor
    {
        /// <summary>
        /// 压缩并存储对话历史
        /// Compress and store dialog history
        /// </summary>
        Task<ContextCompressionResult> CompressAndStoreAsync(
            DialogContext context,
            CancellationToken ct = default);

        /// <summary>
        /// 生成对话摘要（使用LLM）
        /// Generate dialog summary using LLM
        /// </summary>
        Task<string> GenerateSummaryAsync(
            List<MessageContext> messages,
            CancellationToken ct = default);

        /// <summary>
        /// 提取关键信息（使用LLM）
        /// Extract key information using LLM
        /// </summary>
        Task<List<KeyInformation>> ExtractKeyInformationAsync(
            List<MessageContext> messages,
            CancellationToken ct = default);
    }
}
