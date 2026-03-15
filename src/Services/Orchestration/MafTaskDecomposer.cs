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

        /// <summary>
        /// 使用LLM进行任务分解
        /// </summary>
        /// <param name="complexTask">复杂任务描述</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>任务分解结果</returns>
        public async Task<TaskDecomposition> DecomposeTaskWithLlmAsync(
            string complexTask,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Using LLM to decompose complex task: {Task}", complexTask);

            var decomposition = new TaskDecomposition
            {
                OriginalUserInput = complexTask,
                Intent = new IntentRecognitionResult
                {
                    PrimaryIntent = "ComplexTask",
                    Confidence = 0.8,
                    OriginalInput = complexTask
                }
            };

            try
            {
                // 构建任务分解的提示词
                var prompt = BuildDecompositionPrompt(complexTask);

                // 注意：这里需要实际的LLM服务调用
                // 由于架构要求使用MS AF，这里需要通过注入的LLM agent来调用
                // 暂时使用基于规则的简单实现
                var subtasks = await RuleBasedDecomposition(complexTask);

                decomposition.SubTasks.AddRange(subtasks);
                decomposition.Metadata.Strategy = "LlmAssisted";

                _logger.LogInformation("LLM decomposed task into {Count} subtasks", subtasks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM-based task decomposition failed, falling back to rule-based");
                var subtasks = await RuleBasedDecomposition(complexTask);
                decomposition.SubTasks.AddRange(subtasks);
                decomposition.Metadata.Strategy = "RuleBased";
            }

            return decomposition;
        }

        /// <summary>
        /// 构建任务分解的提示词
        /// </summary>
        private string BuildDecompositionPrompt(string task)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("你是一个任务分解专家。请将用户输入的复杂任务分解为多个可独立执行的子任务。");
            sb.AppendLine();
            sb.AppendLine("用户任务：");
            sb.AppendLine(task);
            sb.AppendLine();
            sb.AppendLine("请按照以下格式输出：");
            sb.AppendLine("1. 子任务名称：XXX");
            sb.AppendLine("   意图：XXX");
            sb.AppendLine("   描述：XXX");
            sb.AppendLine("   优先级：高/中/低");
            sb.AppendLine("   所需能力：XXX");
            sb.AppendLine();
            sb.AppendLine("每个子任务应该：");
            sb.AppendLine("- 可以独立执行");
            sb.AppendLine("- 有明确的目标");
            sb.AppendLine("- 有优先级顺序");
            sb.AppendLine("- 指定所需的能力");

            return sb.ToString();
        }

        /// <summary>
        /// 基于规则的任务分解（回退方案）
        /// </summary>
        private async Task<List<DecomposedTask>> RuleBasedDecomposition(string task)
        {
            var subtasks = new List<DecomposedTask>();

            // 简单的关键词匹配分解策略
            if (task.Contains("并且") || task.Contains("然后") || task.Contains("接着"))
            {
                // 包含连接词，尝试分割
                var parts = task.Split(new[] { "并且", "然后", "接着" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        subtasks.Add(new DecomposedTask
                        {
                            TaskName = $"子任务{i + 1}",
                            Intent = InferIntentFromText(part),
                            Description = part,
                            Priority = i == 0 ? TaskPriority.High : TaskPriority.Normal,
                            PriorityScore = Math.Max(100 - i * 10, 30),
                            RequiredCapability = InferCapabilityFromText(part),
                            ExecutionStrategy = ExecutionStrategy.Immediate, // 使用Immediate替代Sequential
                            Parameters = new Dictionary<string, object>
                            {
                                ["userInput"] = part,
                                ["order"] = i + 1
                            }
                        });
                    }
                }
            }
            else
            {
                // 单一任务
                subtasks.Add(new DecomposedTask
                {
                    TaskName = "主任务",
                    Intent = InferIntentFromText(task),
                    Description = task,
                    Priority = TaskPriority.High,
                    PriorityScore = 80,
                    RequiredCapability = InferCapabilityFromText(task),
                    ExecutionStrategy = ExecutionStrategy.Immediate,
                    Parameters = new Dictionary<string, object>
                    {
                        ["userInput"] = task
                    }
                });
            }

            return await Task.FromResult(subtasks);
        }

        /// <summary>
        /// 从文本推断意图
        /// </summary>
        private string InferIntentFromText(string text)
        {
            if (text.Contains("灯") || text.Contains("照明")) return "ControlLight";
            if (text.Contains("空调") || text.Contains("温度")) return "AdjustClimate";
            if (text.Contains("音乐") || text.Contains("播放")) return "PlayMusic";
            if (text.Contains("查询") || text.Contains("天气")) return "Query";
            return "GeneralQuery";
        }

        /// <summary>
        /// 从文本推断所需能力
        /// </summary>
        private string InferCapabilityFromText(string text)
        {
            if (text.Contains("灯") || text.Contains("照明")) return "lighting";
            if (text.Contains("空调") || text.Contains("温度")) return "climate";
            if (text.Contains("音乐")) return "music";
            return "general";
        }
    }
}
