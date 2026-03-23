using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents.Specialized;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using CKY.MultiAgentFramework.Services.Dialog;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 智能家居主控Agent（继承 MafLeaderAgent）
    /// 在通用编排流水线基础上添加：
    /// - 对话状态管理
    /// - 必需实体澄清
    /// - 槽位自动填充
    /// - 记忆分类
    /// - 上下文压缩
    /// </summary>
    public class SmartHomeLeaderAgent : MafLeaderAgent
    {
        private readonly IDialogStateManager _stateManager;
        private readonly IMemoryClassifier _memoryClassifier;
        private readonly IContextCompressor _contextCompressor;

        // 当前轮次的对话上下文，在 ExecuteBusinessLogicAsync 中加载
        private DialogContext? _currentDialogContext;
        private IntentRecognitionResult? _currentIntent;

        public override string AgentId => "smarthome:leader:agent:001";
        public override string Name => "SmartHomeLeaderAgent";
        public override string Description => "智能家居主控Agent，负责协调所有子Agent";
        public override IReadOnlyList<string> Capabilities =>
        [
            "smarthome:coordination",
            "smarthome:task_decomposition",
            "smarthome:agent_orchestration"
        ];

        public SmartHomeLeaderAgent(
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
            ILogger<SmartHomeLeaderAgent> logger)
            : base(intentRecognizer, entityExtractor, taskDecomposer, agentMatcher,
                   taskOrchestrator, resultAggregator, llmRegistry, logger)
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _memoryClassifier = memoryClassifier ?? throw new ArgumentNullException(nameof(memoryClassifier));
            _contextCompressor = contextCompressor ?? throw new ArgumentNullException(nameof(contextCompressor));
        }

        /// <summary>
        /// 重写主入口：在基类流水线前后添加对话状态管理
        /// </summary>
        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request, CancellationToken ct = default)
        {
            // 加载对话上下文
            _currentDialogContext = await _stateManager.LoadOrCreateAsync(
                request.ConversationId, request.UserId, ct);

            Logger.LogInformation("Dialog context loaded: TurnCount={TurnCount}, PreviousIntent={PreviousIntent}",
                _currentDialogContext.TurnCount, _currentDialogContext.PreviousIntent);

            // 执行基类的标准流水线
            return await base.ExecuteBusinessLogicAsync(request, ct);
        }

        /// <summary>
        /// 任务分解前：检查必需实体是否齐全
        /// </summary>
        protected override Task<MafTaskResponse?> OnBeforeDecomposeAsync(
            MafTaskRequest request,
            IntentRecognitionResult intent,
            Dictionary<string, object> entities,
            CancellationToken ct)
        {
            _currentIntent = intent;

            var stringEntities = entities.ToDictionary(
                e => e.Key, e => e.Value?.ToString() ?? "", StringComparer.OrdinalIgnoreCase);

            var clarification = CheckRequiredEntities(intent.PrimaryIntent, stringEntities);
            if (clarification != null)
            {
                Logger.LogInformation("Clarification needed for intent {Intent}: {Question}",
                    intent.PrimaryIntent, clarification.Question);

                return Task.FromResult<MafTaskResponse?>(new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    NeedsClarification = true,
                    ClarificationQuestion = clarification.Question,
                    ClarificationOptions = clarification.Options,
                    Result = clarification.Question,
                });
            }

            return Task.FromResult<MafTaskResponse?>(null);
        }

        /// <summary>
        /// 任务执行后：槽位自动填充 + 对话状态更新 + 记忆分类 + 上下文压缩
        /// </summary>
        protected override async Task<List<TaskExecutionResult>> OnAfterExecuteAsync(
            MafTaskRequest request,
            IntentRecognitionResult intent,
            List<TaskExecutionResult> results,
            CancellationToken ct)
        {
            // 槽位自动填充：对缺失槽位的失败任务从历史中补全
            if (_currentDialogContext != null)
            {
                results = await TryAutoFillSlotsAsync(request, intent, results, ct);
            }

            // 更新对话状态
            if (_currentDialogContext != null)
            {
                var slotDict = request.Parameters.ToDictionary(p => p.Key, p => p.Value);
                await _stateManager.UpdateAsync(_currentDialogContext, intent.PrimaryIntent, slotDict, results, ct);

                // 记忆分类
                var classificationResult = await _memoryClassifier.ClassifyAndStoreAsync(
                    intent.PrimaryIntent, slotDict, _currentDialogContext, ct);

                Logger.LogInformation("Memory classification: {LongTerm} long-term, {ShortTerm} short-term",
                    classificationResult.LongTermMemories.Count, classificationResult.ShortTermMemories.Count);

                // 每5轮触发上下文压缩
                if (_currentDialogContext.TurnCount > 0 && _currentDialogContext.TurnCount % 5 == 0)
                {
                    Logger.LogInformation("Triggering context compression (TurnCount={TurnCount})",
                        _currentDialogContext.TurnCount);
                    var compressionResult = await _contextCompressor.CompressAndStoreAsync(_currentDialogContext, ct);
                    Logger.LogInformation("Context compression complete: ratio={Ratio:0.2}",
                        compressionResult.CompressionRatio);
                }
            }

            return results;
        }

        /// <summary>
        /// 自定义低置信度响应
        /// </summary>
        protected override MafTaskResponse BuildLowConfidenceResponse(
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

        #region 私有辅助方法

        private async Task<List<TaskExecutionResult>> TryAutoFillSlotsAsync(
            MafTaskRequest request,
            IntentRecognitionResult intent,
            List<TaskExecutionResult> results,
            CancellationToken ct)
        {
            foreach (var result in results)
            {
                if (!result.Success && !string.IsNullOrEmpty(result.Error) &&
                    result.Error.Contains("slot", StringComparison.OrdinalIgnoreCase) &&
                    result.Error.Contains("missing", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogWarning("SubAgent {TaskId} reported missing slots. Attempting auto-fill from history.",
                        result.TaskId);

                    var filledCount = 0;
                    foreach (var kvp in _currentDialogContext!.HistoricalSlots)
                    {
                        if (!request.Parameters.ContainsKey(kvp.Key))
                        {
                            var parts = kvp.Key.Split('.');
                            if (parts.Length == 2 && parts[0] == intent.PrimaryIntent)
                            {
                                var slotName = parts[1];
                                request.Parameters[slotName] = kvp.Value;
                                filledCount++;
                                Logger.LogInformation("Auto-filled slot {Slot}={Value} from history (intent={Intent})",
                                    slotName, kvp.Value, intent.PrimaryIntent);
                            }
                        }
                    }

                    if (filledCount > 0)
                    {
                        Logger.LogInformation("Auto-filled {Count} slots, retrying tasks", filledCount);
                        // Re-run the tasks via the base class method
                        return await base.OnAfterExecuteAsync(request, intent, results, ct);
                    }
                }
            }
            return results;
        }

        private static ClarificationInfo? CheckRequiredEntities(
            string intent, Dictionary<string, string> entities)
        {
            return intent switch
            {
                "QueryWeather" => CheckWeatherEntities(entities),
                "QueryTemperatureHistory" => CheckTempHistoryEntities(entities),
                _ => null
            };
        }

        private static ClarificationInfo? CheckWeatherEntities(Dictionary<string, string> entities)
        {
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

        private sealed class ClarificationInfo
        {
            public string Question { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
        }

        #endregion
    }
}
