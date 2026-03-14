using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Orchestration
{
    /// <summary>
    /// 任务分解服务
    /// 将用户输入的复杂任务分解为多个可独立执行的子任务
    /// </summary>
    public class MafTaskDecomposer : ITaskDecomposer
    {
        private readonly ILogger<MafTaskDecomposer> _logger;
        private readonly IIntentCapabilityProvider _capabilityProvider;

        public MafTaskDecomposer(
            IIntentCapabilityProvider capabilityProvider,
            ILogger<MafTaskDecomposer> logger)
        {
            _capabilityProvider = capabilityProvider ?? throw new ArgumentNullException(nameof(capabilityProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<TaskDecomposition> DecomposeAsync(
            string userInput,
            IntentRecognitionResult intent,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Decomposing task for intent: {Intent}", intent.PrimaryIntent);

            var decomposition = new TaskDecomposition
            {
                OriginalUserInput = userInput,
                Intent = intent
            };

            // 根据主意图创建主任务
            var capability = _capabilityProvider.GetCapability(intent.PrimaryIntent);

            if (capability != null)
            {
                var primaryTask = new DecomposedTask
                {
                    TaskName = $"{intent.PrimaryIntent}主任务",
                    Intent = intent.PrimaryIntent,
                    Description = $"处理用户请求: {userInput}",
                    Priority = TaskPriority.Normal,
                    PriorityScore = 50,
                    RequiredCapability = capability,
                    ExecutionStrategy = ExecutionStrategy.Immediate,
                    Parameters = new Dictionary<string, object>
                    {
                        ["userInput"] = userInput,
                        ["intent"] = intent.PrimaryIntent,
                        ["confidence"] = intent.Confidence
                    }
                };
                decomposition.SubTasks.Add(primaryTask);
            }
            else
            {
                // 未知意图，创建通用任务
                var genericTask = new DecomposedTask
                {
                    TaskName = "通用查询任务",
                    Intent = intent.PrimaryIntent,
                    Description = $"处理用户请求: {userInput}",
                    Priority = TaskPriority.Normal,
                    PriorityScore = 30,
                    RequiredCapability = "general",
                    ExecutionStrategy = ExecutionStrategy.Immediate,
                    Parameters = new Dictionary<string, object>
                    {
                        ["userInput"] = userInput
                    }
                };
                decomposition.SubTasks.Add(genericTask);
            }

            decomposition.Metadata.Strategy = "RuleBased";
            return Task.FromResult(decomposition);
        }
    }
}
