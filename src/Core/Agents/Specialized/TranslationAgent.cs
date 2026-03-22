using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 翻译 Agent
    /// 负责文本翻译和多语言处理
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 文本翻译：在多种语言之间翻译文本
    /// - 本地化：为产品或服务进行多语言适配
    /// - 文档翻译：技术文档、用户手册的翻译
    /// - 实时翻译：对话或会议的实时翻译
    /// </remarks>
    public class TranslationAgent : MafBusinessAgentBase
    {
        public override string AgentId => "translation-agent-001";
        public override string Name => "TranslationAgent";
        public override string Description => "翻译Agent，提供多语言翻译和本地化服务";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "text-translation",
            "localization",
            "multilingual",
            "language-detection"
        };

        public TranslationAgent(
            IMafAiAgentRegistry llmRegistry,
            ILogger<TranslationAgent> logger)
            : base(llmRegistry, logger)
        {
        }

        /// <summary>
        /// 执行业务逻辑：文本翻译
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

                Logger.LogInformation("[Translation] 开始翻译，原文长度: {Length}", text.Length);

                // 提取参数
                var targetLanguage = GetParameter(request, "targetLanguage", "en");
                var sourceLanguage = GetParameter(request, "sourceLanguage", "auto");
                var style = GetParameter(request, "style", "formal"); // formal, casual, technical

                // 构建提示词
                var prompt = BuildTranslationPrompt(text, sourceLanguage, targetLanguage, style);

                // 调用 LLM
                var translation = await CallLlmAsync(prompt, LlmScenario.Translation, null, ct);

                Logger.LogInformation("[Translation] 翻译完成，目标语言: {Language}", targetLanguage);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = translation,
                    Data = new Dictionary<string, object>
                    {
                        ["translation"] = translation,
                        ["original"] = text,
                        ["source_language"] = sourceLanguage,
                        ["target_language"] = targetLanguage,
                        ["style"] = style
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Translation] 翻译失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"翻译失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 构建翻译提示词
        /// </summary>
        private string BuildTranslationPrompt(string text, string sourceLanguage, string targetLanguage, string style)
        {
            var styleInstruction = style switch
            {
                "formal" => "使用正式、专业的语气",
                "casual" => "使用随意、口语化的语气",
                "technical" => "使用准确的技术术语",
                _ => "保持原文风格"
            };

            var sourceInstruction = sourceLanguage == "auto" ? "自动检测源语言" : $"从{sourceLanguage}翻译";
            var targetInstruction = GetLanguageName(targetLanguage);

            return $"请将以下文本{sourceInstruction}为{targetInstruction}。{styleInstruction}。\n\n原文：{text}";
        }

        /// <summary>
        /// 获取语言的完整名称
        /// </summary>
        private string GetLanguageName(string code)
        {
            return code switch
            {
                "zh" => "中文",
                "en" => "英文",
                "ja" => "日文",
                "ko" => "韩文",
                "fr" => "法文",
                "de" => "德文",
                "es" => "西班牙文",
                "ru" => "俄文",
                "ar" => "阿拉伯文",
                _ => code
            };
        }
    }
}
