using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 意图识别 Agent
    /// 负责识别用户输入的意图和目的
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 意图分类：将用户输入分类到预定义的意图类别
    /// - 意图置信度：评估识别结果的可信度
    /// - 多意图识别：识别一句话中的多个意图
    /// - 意图澄清：当意图不明确时提出澄清问题
    /// </remarks>
    public class IntentRecognitionAgent : MafBusinessAgentBase
    {
        public override string AgentId => "intent-recognition-agent-001";
        public override string Name => "IntentRecognitionAgent";
        public override string Description => "意图识别Agent，识别用户输入的意图和目的";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "intent-classification",
            "intent-confidence",
            "multi-intent-detection",
            "intent-clarification"
        };

        private readonly IIntentKeywordProvider _keywordProvider;

        public IntentRecognitionAgent(
            IMafAiAgentRegistry llmRegistry,
            IIntentKeywordProvider keywordProvider,
            ILogger<IntentRecognitionAgent> logger)
            : base(llmRegistry, logger)
        {
            _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
        }

        /// <summary>
        /// 执行业务逻辑：意图识别
        /// </summary>
        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var input = request.UserInput;
                if (string.IsNullOrWhiteSpace(input))
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Error = "输入不能为空"
                    };
                }

                Logger.LogInformation("[IntentRecognition] 开始识别意图: {Input}", input);

                // 获取支持的意图类型
                var supportedIntents = _keywordProvider.GetSupportedIntents().ToArray();

                // 构建提示词
                var prompt = BuildIntentRecognitionPrompt(input, supportedIntents, request);

                // 调用 LLM
                var result = await CallLlmAsync(prompt, LlmScenario.Intent, null, ct);

                // 解析结果
                var recognitionResult = ParseIntentResult(result);

                Logger.LogInformation("[IntentRecognition] 意图识别完成: {Intent} (置信度: {Confidence})",
                    recognitionResult.PrimaryIntent, recognitionResult.Confidence);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = result,
                    Data = new Dictionary<string, object>
                    {
                        ["primary_intent"] = recognitionResult.PrimaryIntent,
                        ["confidence"] = recognitionResult.Confidence,
                        ["all_intents"] = recognitionResult.AllIntents.ToList(),
                        ["needs_clarification"] = recognitionResult.NeedsClarification,
                        ["clarification_question"] = recognitionResult.ClarificationQuestion ?? string.Empty
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[IntentRecognition] 意图识别失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"意图识别失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 构建意图识别提示词
        /// </summary>
        private string BuildIntentRecognitionPrompt(string input, string[] supportedIntents, MafTaskRequest request)
        {
            var mode = GetParameter(request, "mode", "single"); // single, multi, clarify

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("你是专业的意图识别助手。请分析用户输入，识别其主要意图。");
            sb.AppendLine();

            sb.AppendLine("支持的意图类型：");
            foreach (var intent in supportedIntents)
            {
                var keywords = _keywordProvider.GetKeywords(intent);
                if (keywords != null && keywords.Length > 0)
                {
                    var sampleKeywords = string.Join("、", keywords.Take(3));
                    sb.AppendLine($"- {intent}: {sampleKeywords}");
                }
                else
                {
                    sb.AppendLine($"- {intent}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"用户输入：{input}");
            sb.AppendLine();

            if (mode == "multi")
            {
                sb.AppendLine("请识别用户输入中的所有意图（可能包含多个）。");
                sb.AppendLine("返回JSON格式：");
                sb.AppendLine(@"{""primary_intent"": ""主要意图"", ""confidence"": 0.95, ""all_intents"": [""意图1"", ""意图2""]}");
            }
            else if (mode == "clarify")
            {
                sb.AppendLine("如果意图不明确，请提出澄清问题。");
                sb.AppendLine("返回JSON格式：");
                sb.AppendLine(@"{""primary_intent"": ""意图"", ""confidence"": 0.5, ""needs_clarification"": true, ""clarification_question"": ""澄清问题""}");
            }
            else
            {
                sb.AppendLine("请识别用户输入的主要意图。");
                sb.AppendLine("返回JSON格式：");
                sb.AppendLine(@"{""primary_intent"": ""意图名称"", ""confidence"": 0.95}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 解析意图识别结果
        /// </summary>
        private IntentRecognitionResult ParseIntentResult(string llmResponse)
        {
            var result = new IntentRecognitionResult();

            try
            {
                var jsonStart = llmResponse.IndexOf('{');
                var jsonEnd = llmResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonStr = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = System.Text.Json.JsonDocument.Parse(jsonStr);

                    if (parsed.RootElement.TryGetProperty("primary_intent", out var intentProp))
                    {
                        result.PrimaryIntent = intentProp.GetString() ?? "Unknown";
                    }

                    if (parsed.RootElement.TryGetProperty("confidence", out var confidenceProp))
                    {
                        result.Confidence = confidenceProp.GetDouble();
                    }

                    if (parsed.RootElement.TryGetProperty("all_intents", out var allIntentsProp))
                    {
                        result.AllIntents = allIntentsProp.EnumerateArray()
                            .Select(x => x.GetString())
                            .OfType<string>() // Filter out nulls
                            .ToArray()!;
                    }

                    if (parsed.RootElement.TryGetProperty("needs_clarification", out var clarifyProp))
                    {
                        result.NeedsClarification = clarifyProp.GetBoolean();
                    }

                    if (parsed.RootElement.TryGetProperty("clarification_question", out var questionProp))
                    {
                        result.ClarificationQuestion = questionProp.GetString();
                    }
                }
                else
                {
                    result.PrimaryIntent = "Unknown";
                    result.Confidence = 0.0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "解析意图识别结果失败");
                result.PrimaryIntent = "Unknown";
                result.Confidence = 0.0;
            }

            return result;
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

        /// <summary>
        /// 意图识别结果
        /// </summary>
        private class IntentRecognitionResult
        {
            public string PrimaryIntent { get; set; } = "Unknown";
            public double Confidence { get; set; }
            public string[] AllIntents { get; set; } = Array.Empty<string>();
            public bool NeedsClarification { get; set; }
            public string? ClarificationQuestion { get; set; }
        }
    }
}
