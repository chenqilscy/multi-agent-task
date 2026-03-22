using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Agents
{
    /// <summary>
    /// 知识库Agent
    /// 基于RAG管线，提供智能家居常见问题解答和帮助文档查询
    /// 支持LLM增强：将RAG检索结果通过LLM生成连贯的自然语言回答
    /// </summary>
    public class KnowledgeBaseAgent : MafBusinessAgentBase
    {
        private readonly IRagPipeline _ragPipeline;

        /// <summary>知识库集合名称</summary>
        public const string CollectionName = "smarthome-knowledge";

        public override string AgentId => "knowledge-base-agent-001";
        public override string Name => "KnowledgeBaseAgent";
        public override string Description => "智能家居知识库Agent，支持FAQ查询和帮助文档检索";
        public override IReadOnlyList<string> Capabilities => ["knowledge-base", "faq", "help"];

        public KnowledgeBaseAgent(
            IRagPipeline ragPipeline,
            IMafAiAgentRegistry llmRegistry,
            ILogger<KnowledgeBaseAgent> logger)
            : base(llmRegistry, logger)
        {
            _ragPipeline = ragPipeline ?? throw new ArgumentNullException(nameof(ragPipeline));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            var userInput = request.UserInput;

            try
            {
                Logger.LogInformation("知识库查询: {Query}", userInput);

                var queryRequest = new RagQueryRequest
                {
                    Query = userInput,
                    CollectionName = CollectionName,
                    TopK = 3,
                    ScoreThreshold = 0.3f
                };

                var result = await _ragPipeline.QueryAsync(queryRequest, ct);

                if (result.RetrievedChunks.Count == 0)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = true,
                        Result = "抱歉，我在知识库中没有找到相关内容。您可以尝试用不同的方式描述问题，或者使用对话功能直接控制设备。"
                    };
                }

                // 构建知识上下文
                var knowledgeContext = string.Join("\n\n", result.RetrievedChunks.Select(c => c.Content));

                // 尝试通过LLM生成连贯回答
                var answer = await GenerateLlmAnswerAsync(userInput, knowledgeContext, ct);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = answer
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "知识库查询失败: {Query}", userInput);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = "知识库查询发生错误",
                    Result = "抱歉，知识库查询时发生错误，请稍后重试。"
                };
            }
        }

        /// <summary>
        /// 通过LLM对RAG检索结果进行智能总结，生成自然语言回答
        /// 如果LLM不可用，降级为直接拼接知识片段
        /// </summary>
        private async Task<string> GenerateLlmAnswerAsync(string question, string knowledgeContext, CancellationToken ct)
        {
            try
            {
                var prompt = $"请根据以下知识库内容，回答用户的问题。要求简洁、准确、友好。\n\n" +
                             $"【知识库内容】\n{knowledgeContext}\n\n" +
                             $"【用户问题】\n{question}\n\n" +
                             $"【回答要求】\n基于知识库内容回答，不要编造信息。如果知识库内容不足以完整回答，请说明。";

                var llmAnswer = await CallLlmAsync(
                    prompt,
                    LlmScenario.Chat,
                    "你是智能家居系统的知识库助手，专注于回答用户关于智能家居设备使用的问题。",
                    ct);

                if (!string.IsNullOrWhiteSpace(llmAnswer))
                {
                    Logger.LogInformation("LLM增强回答生成成功");
                    return llmAnswer;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "LLM增强回答生成失败，降级为知识片段拼接");
            }

            // 降级：直接拼接知识片段
            return $"根据知识库查询结果：\n\n{knowledgeContext}";
        }
    }
}
