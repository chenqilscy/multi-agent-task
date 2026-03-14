using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 文本摘要 Agent
    /// 负责生成长文本的摘要、提取关键信息
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 文章摘要：为长篇文章生成简短摘要
    /// - 会议记录：总结会议要点和决策
    /// - 文档提炼：从技术文档中提取关键信息
    /// - 新闻聚合：快速了解多条新闻的核心内容
    /// </remarks>
    public class SummarizationAgent : MafAgentBase
    {
        public override string AgentId => "summarization-agent-001";
        public override string Name => "SummarizationAgent";
        public override string Description => "文本摘要Agent，生成长文本的摘要和关键信息提取";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "text-summarization",
            "key-extraction",
            "abstractive-summarization",
            "extractive-summarization"
        };

        public SummarizationAgent(
            ILlmAgentRegistry llmRegistry,
            ILogger<SummarizationAgent> logger)
            : base(llmRegistry, logger)
        {
        }

        /// <summary>
        /// 执行业务逻辑：文本摘要
        /// </summary>
        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var text = request.UserInput;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Error = "输入文本不能为空"
                    };
                }

                Logger.LogInformation("[Summarization] 开始生成摘要，原文长度: {Length}", text.Length);

                // 提取参数
                var summaryType = GetParameter(request, "type", "brief"); // brief, detailed, bullet
                var maxLength = GetParameter(request, "maxLength", 200);
                var language = GetParameter(request, "language", "zh");

                // 构建提示词
                var prompt = BuildSummarizationPrompt(text, summaryType, maxLength, language);

                // 调用 LLM
                var summary = await CallLlmAsync(prompt, LlmScenario.Summarization, null, ct);

                Logger.LogInformation("[Summarization] 摘要生成完成，摘要长度: {Length}", summary.Length);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = summary,
                    Data = new Dictionary<string, object>
                    {
                        ["summary"] = summary,
                        ["original_length"] = text.Length,
                        ["summary_length"] = summary.Length,
                        ["compression_ratio"] = Math.Round((double)summary.Length / text.Length, 2),
                        ["type"] = summaryType
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Summarization] 摘要生成失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"摘要生成失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 构建摘要生成提示词
        /// </summary>
        private string BuildSummarizationPrompt(string text, string type, int maxLength, string language)
        {
            var typeInstruction = type switch
            {
                "brief" => "生成一个简短的摘要（2-3句话）",
                "detailed" => "生成一个详细的摘要（包含主要观点和细节）",
                "bullet" => "以项目符号列表的形式生成摘要",
                _ => "生成一个简短的摘要"
            };

            var languageInstruction = language == "zh" ? "请用中文回答" : "Please answer in English";

            return $"{typeInstruction}，最多{maxLength}个字。\n\n{text}\n\n{languageInstruction}";
        }

        /// <summary>
        /// 从请求中提取参数
        /// </summary>
        private T GetParameter<T>(MafTaskRequest request, string key, T defaultValue)
        {
            if (request.Parameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
