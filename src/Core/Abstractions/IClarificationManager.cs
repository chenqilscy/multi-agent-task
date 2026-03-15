using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 澄清管理器接口，负责分析澄清需求、生成澄清问题并处理用户响应
    /// Clarification manager interface, responsible for analyzing clarification needs, generating clarification questions, and processing user responses
    /// </summary>
    public interface IClarificationManager
    {
        /// <summary>
        /// 分析是否需要澄清以及采用何种策略
        /// Analyze whether clarification is needed and which strategy to use
        /// </summary>
        /// <param name="slotDetection">槽位检测结果</param>
        /// <param name="context">对话上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>澄清分析结果</returns>
        Task<ClarificationAnalysis> AnalyzeClarificationNeededAsync(
            SlotDetectionResult slotDetection,
            DialogContext context,
            CancellationToken ct = default);

        /// <summary>
        /// 生成澄清问题
        /// Generate clarification question
        /// </summary>
        /// <param name="clarificationContext">澄清上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>生成的澄清问题文本</returns>
        Task<string> GenerateClarificationQuestionAsync(
            ClarificationContext clarificationContext,
            CancellationToken ct = default);

        /// <summary>
        /// 处理用户对澄清问题的响应
        /// Process user's response to clarification question
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <param name="clarificationContext">澄清上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>澄清响应结果</returns>
        Task<ClarificationResponse> ProcessUserResponseAsync(
            string userInput,
            ClarificationContext clarificationContext,
            CancellationToken ct = default);
    }
}
