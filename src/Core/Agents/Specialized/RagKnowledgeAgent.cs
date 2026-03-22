using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 通用 RAG 知识库 Agent（内置）
    /// 提供 RAG 检索 → LLM 增强回答的标准管道
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// - 直接使用：注入 IRagPipeline 后，通过构造函数或属性配置 CollectionName/TopK/ScoreThreshold
    /// - 继承定制：覆盖 FormatRagPrompt / SystemPrompt 实现领域特定的提示词
    ///
    /// 对比 Demo 实现：
    /// - SmartHome.KnowledgeBaseAgent 和 CustomerService.KnowledgeBaseAgent 的 RAG+LLM 逻辑与本类相同
    /// - 领域特定的 prompt 和兜底文案通过覆盖 virtual 属性/方法实现
    /// </remarks>
    public class RagKnowledgeAgent : MafBusinessAgentBase
    {
        private readonly IRagPipeline _ragPipeline;

        /// <summary>知识库集合名称（子类可覆盖）</summary>
        public virtual string CollectionName { get; init; } = "default-knowledge";

        /// <summary>检索返回的最大文档片段数</summary>
        public virtual int TopK { get; init; } = 3;

        /// <summary>相似度分数阈值，低于此值的片段会被过滤</summary>
        public virtual float ScoreThreshold { get; init; } = 0.3f;

        /// <summary>LLM 系统提示词（子类可覆盖以定制领域角色）</summary>
        protected virtual string SystemPrompt =>
            "你是一个知识库助手，根据检索到的内容回答用户问题。";

        /// <summary>知识库无结果时的兜底文案（子类可覆盖）</summary>
        protected virtual string NoResultMessage =>
            "抱歉，我在知识库中没有找到相关内容。请尝试用不同的方式描述问题。";

        public override string AgentId => "maf:rag-knowledge-agent:builtin";
        public override string Name => "RagKnowledgeAgent";
        public override string Description => "通用 RAG 知识库 Agent，支持检索增强生成";
        public override IReadOnlyList<string> Capabilities =>
            ["knowledge-base", "faq", "rag-query"];

        public RagKnowledgeAgent(
            IRagPipeline ragPipeline,
            IMafAiAgentRegistry llmRegistry,
            ILogger<RagKnowledgeAgent> logger)
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
                Logger.LogInformation("[RagKnowledge] Query: {Query}, Collection: {Collection}",
                    userInput, CollectionName);

                var queryRequest = new RagQueryRequest
                {
                    Query = userInput,
                    CollectionName = CollectionName,
                    TopK = TopK,
                    ScoreThreshold = ScoreThreshold
                };

                var result = await _ragPipeline.QueryAsync(queryRequest, ct);

                if (result.RetrievedChunks.Count == 0)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = true,
                        Result = NoResultMessage
                    };
                }

                var knowledgeContext = string.Join("\n\n",
                    result.RetrievedChunks.Select(c => c.Content));

                var answer = await GenerateAnswerAsync(userInput, knowledgeContext, ct);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = answer
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[RagKnowledge] Query failed: {Query}", userInput);
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
        /// 构造 RAG 提示词（子类可覆盖以定制提示词模板）
        /// </summary>
        protected virtual string FormatRagPrompt(string question, string knowledgeContext)
        {
            return $"请根据以下知识库内容，回答用户的问题。要求简洁、准确、友好。\n\n" +
                   $"【知识库内容】\n{knowledgeContext}\n\n" +
                   $"【用户问题】\n{question}\n\n" +
                   $"【回答要求】\n基于知识库内容回答，不要编造信息。如果知识库内容不足以完整回答，请说明。";
        }

        /// <summary>
        /// 生成回答：LLM 增强 + 降级到知识片段拼接
        /// </summary>
        private async Task<string> GenerateAnswerAsync(
            string question, string knowledgeContext, CancellationToken ct)
        {
            try
            {
                var prompt = FormatRagPrompt(question, knowledgeContext);
                var llmAnswer = await CallLlmAsync(prompt, LlmScenario.Chat, SystemPrompt, ct);

                if (!string.IsNullOrWhiteSpace(llmAnswer))
                {
                    Logger.LogInformation("[RagKnowledge] LLM-enhanced answer generated");
                    return llmAnswer;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[RagKnowledge] LLM enhancement failed, falling back to raw chunks");
            }

            return $"根据知识库查询结果：\n\n{knowledgeContext}";
        }
    }
}
