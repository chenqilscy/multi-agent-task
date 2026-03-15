using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using CKY.MultiAgentFramework.Services.Dialog;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 智能家居主控Agent
    /// 负责任务分解、Agent编排和结果聚合
    /// </summary>
    public class SmartHomeMainAgent : CKY.MultiAgentFramework.Core.Agents.MafBusinessAgentBase
    {
        private readonly IIntentRecognizer _intentRecognizer;
        private readonly ITaskDecomposer _taskDecomposer;
        private readonly IAgentMatcher _agentMatcher;
        private readonly ITaskOrchestrator _taskOrchestrator;
        private readonly IResultAggregator _resultAggregator;
        private readonly IEntityExtractor _entityExtractor;
        private readonly IDialogStateManager _stateManager;
        private readonly IMemoryClassifier _memoryClassifier;
        private readonly IContextCompressor _contextCompressor;

        public override string AgentId => "smarthome:main:agent:001";
        public override string Name => "SmartHomeMainAgent";
        public override string Description => "智能家居主控Agent，负责协调所有子Agent";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "smarthome:coordination",
            "smarthome:task_decomposition",
            "smarthome:agent_orchestration"
        };

        public SmartHomeMainAgent(
            IIntentRecognizer intentRecognizer,
            ITaskDecomposer taskDecomposer,
            IAgentMatcher agentMatcher,
            ITaskOrchestrator taskOrchestrator,
            IResultAggregator resultAggregator,
            IEntityExtractor entityExtractor,
            IDialogStateManager stateManager,
            IMemoryClassifier memoryClassifier,
            IContextCompressor contextCompressor,
            IMafAiAgentRegistry llmRegistry,
            ILogger<SmartHomeMainAgent> logger)
            : base(llmRegistry, logger)
        {
            _intentRecognizer = intentRecognizer ?? throw new ArgumentNullException(nameof(intentRecognizer));
            _taskDecomposer = taskDecomposer ?? throw new ArgumentNullException(nameof(taskDecomposer));
            _agentMatcher = agentMatcher ?? throw new ArgumentNullException(nameof(agentMatcher));
            _taskOrchestrator = taskOrchestrator ?? throw new ArgumentNullException(nameof(taskOrchestrator));
            _resultAggregator = resultAggregator ?? throw new ArgumentNullException(nameof(resultAggregator));
            _entityExtractor = entityExtractor ?? throw new ArgumentNullException(nameof(entityExtractor));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _memoryClassifier = memoryClassifier ?? throw new ArgumentNullException(nameof(memoryClassifier));
            _contextCompressor = contextCompressor ?? throw new ArgumentNullException(nameof(contextCompressor));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            Logger.LogInformation("MainAgent processing request: {UserInput}", request.UserInput);

            try
            {
                // 0. 加载对话上下文（新增）
                var dialogContext = await _stateManager.LoadOrCreateAsync(
                    request.ConversationId,
                    request.UserId,
                    ct);

                Logger.LogInformation("Dialog context loaded: TurnCount={TurnCount}, PreviousIntent={PreviousIntent}",
                    dialogContext.TurnCount, dialogContext.PreviousIntent);

                // 1. 意图识别
                var intent = await _intentRecognizer.RecognizeAsync(request.UserInput, ct);
                Logger.LogInformation("Recognized intent: {Intent} (confidence: {Confidence})",
                    intent.PrimaryIntent, intent.Confidence);

                // 意图置信度过低时，请求用户澄清
                if (intent.Confidence < 0.3)
                {
                    return BuildLowConfidenceResponse(request.TaskId, intent);
                }

                // 2. 实体提取
                var extractionResult = await _entityExtractor.ExtractAsync(request.UserInput, ct);
                var entities = extractionResult.ExtractedEntities
                    .GroupBy(e => e.EntityType)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(e => e.Confidence).First().EntityValue,
                        StringComparer.OrdinalIgnoreCase);

                // 3. 澄清判断：检查必需实体是否齐全
                var clarification = CheckRequiredEntities(intent.PrimaryIntent, entities);
                if (clarification != null)
                {
                    Logger.LogInformation("Clarification needed for intent {Intent}: {Question}",
                        intent.PrimaryIntent, clarification.Question);

                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        NeedsClarification = true,
                        ClarificationQuestion = clarification.Question,
                        ClarificationOptions = clarification.Options,
                        Result = clarification.Question,
                    };
                }

                // 4. 将提取到的实体注入请求参数（覆盖现有值以确保实体最新）
                foreach (var entity in entities)
                    request.Parameters[entity.Key] = entity.Value;

                // 5. 任务分解
                var decomposition = await _taskDecomposer.DecomposeAsync(request.UserInput, intent, ct);
                Logger.LogInformation("Decomposed into {Count} sub-tasks", decomposition.SubTasks.Count);

                if (decomposition.SubTasks.Count == 0)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Result = "抱歉，我无法理解您的请求"
                    };
                }

                // 6. Agent匹配
                var agentMapping = await _agentMatcher.MatchBatchAsync(decomposition.SubTasks, ct);

                // 7. 任务编排与执行（独立子任务并行，有依赖的串行）
                var executionPlan = await _taskOrchestrator.CreatePlanAsync(decomposition.SubTasks, ct);
                var executionResults = await _taskOrchestrator.ExecutePlanAsync(executionPlan, ct);

                // 8. 结果聚合
                var aggregated = await _resultAggregator.AggregateAsync(
                    executionResults,
                    request.UserInput,
                    ct);

                // 8.5 更新对话状态和进行记忆分类（新增）
                var slotDict = request.Parameters.ToDictionary(p => p.Key, p => p.Value);
                await _stateManager.UpdateAsync(
                    dialogContext,
                    intent.PrimaryIntent,
                    slotDict,
                    executionResults,
                    ct);

                // 8.6 记忆分类（新增）
                var classificationResult = await _memoryClassifier.ClassifyAndStoreAsync(
                    intent.PrimaryIntent,
                    slotDict,
                    dialogContext,
                    ct);

                Logger.LogInformation("Memory classification: {LongTerm} long-term, {ShortTerm} short-term",
                    classificationResult.LongTermMemories.Count, classificationResult.ShortTermMemories.Count);

                // 8.7 每5轮触发上下文压缩（新增）
                if (dialogContext.TurnCount > 0 && dialogContext.TurnCount % 5 == 0)
                {
                    Logger.LogInformation("Triggering context compression (TurnCount={TurnCount})", dialogContext.TurnCount);
                    var compressionResult = await _contextCompressor.CompressAndStoreAsync(dialogContext, ct);
                    Logger.LogInformation("Context compression complete: ratio={Ratio:0.2}", compressionResult.CompressionRatio);
                }

                // 9. 生成响应
                var response = new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = aggregated.Success,
                    Result = await _resultAggregator.GenerateResponseAsync(aggregated, ct),
                    Data = aggregated.AggregatedData,
                    SubTaskResults = executionResults.Select(r => new SubTaskResult
                    {
                        TaskId = r.TaskId,
                        Success = r.Success,
                        Message = r.Message,
                        Error = r.Error
                    }).ToList()
                };

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "MainAgent failed to process request");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Result = "处理您的请求时遇到问题，请稍后重试",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 针对不同意图检查必需实体是否齐全
        /// </summary>
        private static ClarificationInfo? CheckRequiredEntities(
            string intent, Dictionary<string, string> entities)
        {
            return intent switch
            {
                "QueryWeather" => CheckWeatherEntities(entities),
                "QueryTemperatureHistory" => CheckTempHistoryEntities(entities),
                _ => null // 其他意图暂不强制澄清
            };
        }

        private static ClarificationInfo? CheckWeatherEntities(Dictionary<string, string> entities)
        {
            // 天气查询必需：城市
            if (!entities.ContainsKey("City") || string.IsNullOrWhiteSpace(entities["City"]))
            {
                return new ClarificationInfo
                {
                    Question = "请问您想查询哪个城市的天气？",
                    Options = ["北京", "上海", "广州", "成都", "杭州"],
                };
            }
            return null;
        }

        private static ClarificationInfo? CheckTempHistoryEntities(Dictionary<string, string> entities)
        {
            // 温度历史查询必需：房间
            if (!entities.ContainsKey("Room") || string.IsNullOrWhiteSpace(entities["Room"]))
            {
                return new ClarificationInfo
                {
                    Question = "请问您想查询哪个房间的温度历史？",
                    Options = ["客厅", "卧室", "厨房", "书房"],
                };
            }
            return null;
        }

        private static MafTaskResponse BuildLowConfidenceResponse(
            string taskId, IntentRecognitionResult intent)
        {
            var candidateHints = new List<string>
            {
                "控制设备（如：打开客厅的灯）",
                "查询天气（如：今天北京天气怎么样）",
                "查看温度历史（如：客厅最近的温度变化）",
                "播放音乐（如：播放轻音乐）",
            };

            return new MafTaskResponse
            {
                TaskId = taskId,
                Success = false,
                NeedsClarification = true,
                ClarificationQuestion = "抱歉，我没有完全理解您的意思，您可能想要：",
                ClarificationOptions = candidateHints,
                Result = "抱歉，我没有完全理解您的意思，请尝试更清晰地描述您的需求。",
            };
        }

        /// <summary>澄清信息</summary>
        private sealed class ClarificationInfo
        {
            public string Question { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
        }
    }
}
