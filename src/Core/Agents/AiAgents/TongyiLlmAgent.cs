using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Providers
{
    /// <summary>
    /// 通义千问 LLM Agent 实现（阿里云大语言模型）
    /// </summary>
    /// <remarks>
    /// 模型支持：
    /// - qwen-max: 旗舰模型（最强推理能力）
    /// - qwen-plus: 通用模型（平衡性能和成本）
    /// - qwen-turbo: 快速模型（低成本、低延迟）
    /// - qwen-long: 长文本模型（支持 100 万 tokens）
    ///
    /// API 特点：
    /// - 支持流式和非流式调用
    /// - 支持 function calling（函数调用）
    /// - 支持多轮对话（上下文管理）
    /// - 支持自定义插件
    ///
    /// 使用场景：
    /// - 通用对话和问答
    /// - 代码生成和分析
    /// - 文档理解和总结
    /// - 长文本处理（qwen-long）
    ///
    /// 配置要求：
    /// <code>
    /// var config = new LlmProviderConfig
    /// {
    ///     ProviderName = "Tongyi",
    ///     ApiKey = "your-api-key",
    ///     ModelId = "qwen-max",
    ///     BaseUrl = "https://dashscope.aliyuncs.com/api/v1"
    /// };
    /// </code>
    ///
    /// 实现状态：
    /// - 待实现：通义千问 API 调用（兼容 OpenAI 接口，参考上方文档链接）
    /// - 待实现：流式输出（SSE 流式响应）
    /// - 待实现：Function Calling 支持
    ///
    /// 参考文档：
    /// https://help.aliyun.com/zh/dashscope/developer-reference/compatibility-of-openai-with-dashscope
    /// </remarks>
    public class TongyiLlmAgent : MafAiAgent
    {
        /// <summary>
        /// 初始化 TongyiLlmAgent 类的新实例
        /// </summary>
        /// <param name="config">LLM 配置</param>
        /// <param name="logger">日志记录器</param>
        public TongyiLlmAgent(LlmProviderConfig config, ILogger logger)
            : base(config, logger)
        {
        }

        /// <inheritdoc />
        public override Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            // 在此处实现通义千问 HTTP API 调用
            throw new NotImplementedException("TongyiLlmAgent.ExecuteAsync not yet implemented");
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            // 在此处实现通义千问流式 API 调用
            throw new NotImplementedException("TongyiLlmAgent.ExecuteStreamingAsync not yet implemented");
        }
    }
}
