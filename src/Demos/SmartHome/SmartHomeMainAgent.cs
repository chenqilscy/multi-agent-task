using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 智能家居主控Agent
    /// 负责任务分解、Agent编排和结果聚合
    /// </summary>
    public class SmartHomeMainAgent : CKY.MultiAgentFramework.Core.Agents.MafAgentBase
    {
        private readonly IIntentRecognizer _intentRecognizer;
        private readonly ITaskDecomposer _taskDecomposer;
        private readonly IAgentMatcher _agentMatcher;
        private readonly ITaskOrchestrator _taskOrchestrator;
        private readonly IResultAggregator _resultAggregator;

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
            ILlmAgentRegistry llmRegistry,
            ILogger<SmartHomeMainAgent> logger)
            : base(llmRegistry, logger)
        {
            _intentRecognizer = intentRecognizer ?? throw new ArgumentNullException(nameof(intentRecognizer));
            _taskDecomposer = taskDecomposer ?? throw new ArgumentNullException(nameof(taskDecomposer));
            _agentMatcher = agentMatcher ?? throw new ArgumentNullException(nameof(agentMatcher));
            _taskOrchestrator = taskOrchestrator ?? throw new ArgumentNullException(nameof(taskOrchestrator));
            _resultAggregator = resultAggregator ?? throw new ArgumentNullException(nameof(resultAggregator));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            Logger.LogInformation("MainAgent processing request: {UserInput}", request.UserInput);

            try
            {
                // 1. 意图识别
                var intent = await _intentRecognizer.RecognizeAsync(request.UserInput, ct);
                Logger.LogInformation("Recognized intent: {Intent} (confidence: {Confidence})",
                    intent.PrimaryIntent, intent.Confidence);

                // 2. 任务分解
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

                // 3. Agent匹配
                var agentMapping = await _agentMatcher.MatchBatchAsync(decomposition.SubTasks, ct);

                // 4. 任务编排
                var executionPlan = await _taskOrchestrator.CreatePlanAsync(decomposition.SubTasks, ct);
                var executionResults = await _taskOrchestrator.ExecutePlanAsync(executionPlan, ct);

                // 5. 结果聚合
                var aggregated = await _resultAggregator.AggregateAsync(
                    executionResults,
                    request.UserInput,
                    ct);

                // 6. 生成响应
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
    }
}
