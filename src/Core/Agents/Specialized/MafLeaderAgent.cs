using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 通用主控编排 Agent（内置）
    /// 提供标准的 意图识别 → 实体提取 → 任务分解 → Agent匹配 → 任务执行 → 结果聚合 流水线
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// 1. 提供完整的编排流水线，子类可通过覆盖钩子方法自定义行为
    /// 2. 每个阶段均为 virtual 方法，允许选择性覆盖
    /// 3. 低置信度意图时自动返回澄清请求
    /// 4. 依赖 ITaskOrchestrator 做任务执行（支持并行/串行）
    ///
    /// 使用方式：
    /// - 直接使用：注入所有依赖后即可作为通用编排 Agent
    /// - 继承定制：覆盖钩子方法实现领域特定逻辑
    ///
    /// 对比 Demo 实现：
    /// - SmartHomeLeaderAgent 和 CustomerServiceLeaderAgent 的核心流水线与本类相同
    /// - 领域特定逻辑（情绪检测、设备控制分发等）通过子类覆盖实现
    /// </remarks>
    public class MafLeaderAgent : MafBusinessAgentBase
    {
        private readonly IIntentRecognizer _intentRecognizer;
        private readonly IEntityExtractor _entityExtractor;
        private readonly ITaskDecomposer _taskDecomposer;
        private readonly IAgentMatcher _agentMatcher;
        private readonly ITaskOrchestrator _taskOrchestrator;
        private readonly IResultAggregator _resultAggregator;

        /// <summary>意图置信度阈值，低于此值触发澄清</summary>
        protected virtual double ConfidenceThreshold => 0.3;

        public override string AgentId => "maf:leader-agent:builtin";
        public override string Name => "MafLeaderAgent";
        public override string Description => "通用主控编排Agent，提供意图→分解→执行→聚合的标准流水线";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "coordination",
            "task-decomposition",
            "agent-orchestration",
            "result-aggregation"
        };

        public MafLeaderAgent(
            IIntentRecognizer intentRecognizer,
            IEntityExtractor entityExtractor,
            ITaskDecomposer taskDecomposer,
            IAgentMatcher agentMatcher,
            ITaskOrchestrator taskOrchestrator,
            IResultAggregator resultAggregator,
            IMafAiAgentRegistry llmRegistry,
            ILogger logger)
            : base(llmRegistry, logger)
        {
            _intentRecognizer = intentRecognizer ?? throw new ArgumentNullException(nameof(intentRecognizer));
            _entityExtractor = entityExtractor ?? throw new ArgumentNullException(nameof(entityExtractor));
            _taskDecomposer = taskDecomposer ?? throw new ArgumentNullException(nameof(taskDecomposer));
            _agentMatcher = agentMatcher ?? throw new ArgumentNullException(nameof(agentMatcher));
            _taskOrchestrator = taskOrchestrator ?? throw new ArgumentNullException(nameof(taskOrchestrator));
            _resultAggregator = resultAggregator ?? throw new ArgumentNullException(nameof(resultAggregator));
        }

        /// <summary>
        /// 主入口：执行完整编排流水线
        /// </summary>
        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            using var activity = MafActivitySource.Agent.StartActivity("leader_agent.execute");
            activity?.SetTag("agent.id", AgentId);
            activity?.SetTag("agent.name", Name);

            Logger.LogInformation("[LeaderAgent] Processing: {UserInput}", request.UserInput);

            try
            {
                // 1. 意图识别
                var intent = await RecognizeIntentAsync(request, ct);
                activity?.SetTag("intent.primary", intent.PrimaryIntent);
                activity?.SetTag("intent.confidence", intent.Confidence);

                // 2. 置信度检查
                if (intent.Confidence < ConfidenceThreshold)
                {
                    return BuildLowConfidenceResponse(request.TaskId, intent);
                }

                // 3. 实体提取
                var entities = await ExtractEntitiesAsync(request, ct);
                foreach (var entity in entities)
                {
                    request.Parameters.TryAdd(entity.Key, entity.Value);
                }

                // 4. 前置钩子（子类可覆盖，做澄清/拦截等）
                var preCheckResult = await OnBeforeDecomposeAsync(request, intent, entities, ct);
                if (preCheckResult != null)
                    return preCheckResult;

                // 5. 任务分解
                var decomposition = await DecomposeTaskAsync(request, intent, ct);
                if (decomposition.SubTasks.Count == 0)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Result = "抱歉，我无法理解您的请求"
                    };
                }

                // 6. Agent 匹配
                await _agentMatcher.MatchBatchAsync(decomposition.SubTasks, ct);

                // 7. 任务编排 & 执行
                var executionResults = await ExecuteTasksAsync(decomposition, ct);

                // 8. 后置钩子（子类可覆盖，做重试/降级等）
                executionResults = await OnAfterExecuteAsync(request, intent, executionResults, ct);

                // 9. 结果聚合
                var aggregated = await AggregateResultsAsync(executionResults, request.UserInput, ct);

                Logger.LogInformation("[LeaderAgent] Completed: {TaskId}", request.TaskId);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = aggregated.Success,
                    Result = aggregated.Summary ?? "任务已完成",
                    Data = aggregated.AggregatedData
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[LeaderAgent] Failed to process request");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Result = "抱歉，处理请求时发生错误，请稍后重试。",
                    Error = ex.Message
                };
            }
        }

        #region 可覆盖的流水线阶段

        /// <summary>
        /// 意图识别（可覆盖以添加自定义意图逻辑）
        /// </summary>
        protected virtual Task<IntentRecognitionResult> RecognizeIntentAsync(
            MafTaskRequest request, CancellationToken ct)
        {
            return _intentRecognizer.RecognizeAsync(request.UserInput, ct);
        }

        /// <summary>
        /// 实体提取（可覆盖以添加领域实体）
        /// </summary>
        protected virtual async Task<Dictionary<string, object>> ExtractEntitiesAsync(
            MafTaskRequest request, CancellationToken ct)
        {
            var result = await _entityExtractor.ExtractAsync(request.UserInput, ct);
            return result.ExtractedEntities
                .GroupBy(e => e.EntityType)
                .ToDictionary(
                    g => g.Key,
                    g => (object)g.OrderByDescending(e => e.Confidence).First().EntityValue,
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 任务分解前钩子（可覆盖以做澄清/拦截）
        /// 返回 null 表示继续流水线，返回非 null 表示中断并直接返回该响应
        /// </summary>
        protected virtual Task<MafTaskResponse?> OnBeforeDecomposeAsync(
            MafTaskRequest request,
            IntentRecognitionResult intent,
            Dictionary<string, object> entities,
            CancellationToken ct)
        {
            return Task.FromResult<MafTaskResponse?>(null);
        }

        /// <summary>
        /// 任务分解（可覆盖以自定义分解策略）
        /// </summary>
        protected virtual Task<TaskDecomposition> DecomposeTaskAsync(
            MafTaskRequest request, IntentRecognitionResult intent, CancellationToken ct)
        {
            return _taskDecomposer.DecomposeAsync(request.UserInput, intent, ct);
        }

        /// <summary>
        /// 任务执行（可覆盖以自定义执行逻辑）
        /// </summary>
        protected virtual async Task<List<TaskExecutionResult>> ExecuteTasksAsync(
            TaskDecomposition decomposition, CancellationToken ct)
        {
            var plan = await _taskOrchestrator.CreatePlanAsync(decomposition.SubTasks, ct);
            return await _taskOrchestrator.ExecutePlanAsync(plan, ct);
        }

        /// <summary>
        /// 任务执行后钩子（可覆盖以做重试/降级/槽位自动填充等）
        /// </summary>
        protected virtual Task<List<TaskExecutionResult>> OnAfterExecuteAsync(
            MafTaskRequest request,
            IntentRecognitionResult intent,
            List<TaskExecutionResult> results,
            CancellationToken ct)
        {
            return Task.FromResult(results);
        }

        /// <summary>
        /// 结果聚合（可覆盖以自定义聚合策略）
        /// </summary>
        protected virtual Task<AggregatedResult> AggregateResultsAsync(
            List<TaskExecutionResult> results, string originalInput, CancellationToken ct)
        {
            return _resultAggregator.AggregateAsync(results, originalInput, ct);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 构建低置信度响应
        /// </summary>
        protected virtual MafTaskResponse BuildLowConfidenceResponse(
            string taskId, IntentRecognitionResult intent)
        {
            Logger.LogInformation("[LeaderAgent] Low confidence ({Confidence}) for intent: {Intent}",
                intent.Confidence, intent.PrimaryIntent);

            return new MafTaskResponse
            {
                TaskId = taskId,
                Success = false,
                NeedsClarification = true,
                ClarificationQuestion = "抱歉，我不太确定您的意思。能否请您更详细地描述您想要做什么？",
                Result = "抱歉，我不太确定您的意思。能否请您更详细地描述您想要做什么？"
            };
        }

        #endregion
    }
}
