using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 代码生成 Agent
    /// 负责代码生成、代码审查、代码优化等
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 代码生成：根据需求描述生成代码
    /// - 代码审查：检查代码质量、发现潜在问题
    /// - 代码优化：改进代码性能和可读性
    /// - 代码解释：解释代码的功能和逻辑
    /// - Bug 修复：定位和修复代码错误
    /// </remarks>
    public class CodeAgent : MafBusinessAgentBase
    {
        public override string AgentId => "code-agent-001";
        public override string Name => "CodeAgent";
        public override string Description => "代码生成Agent，提供代码生成、审查、优化等服务";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "code-generation",
            "code-review",
            "code-optimization",
            "bug-fixing",
            "code-explanation"
        };

        public CodeAgent(
            IMafAiAgentRegistry llmRegistry,
            ILogger<CodeAgent> logger)
            : base(llmRegistry, logger)
        {
        }

        /// <summary>
        /// 执行业务逻辑：代码相关任务
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

                // 提取任务类型
                var taskType = GetParameter(request, "taskType", "generate");

                Logger.LogInformation("[Code] 开始处理代码任务: {TaskType}", taskType);

                var prompt = BuildCodePrompt(input, taskType, request);

                // 调用 LLM
                var result = await CallLlmAsync(prompt, LlmScenario.Code, null, ct);

                Logger.LogInformation("[Code] 代码任务完成: {TaskType}", taskType);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = result,
                    Data = new Dictionary<string, object>
                    {
                        ["task_type"] = taskType,
                        ["output"] = result,
                        ["input_length"] = input.Length
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Code] 代码任务失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"代码任务失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 构建代码任务提示词
        /// </summary>
        private string BuildCodePrompt(string input, string taskType, MafTaskRequest request)
        {
            var language = GetParameter(request, "language", "C#");
            var framework = GetParameter(request, "framework", ".NET");

            return taskType switch
            {
                "generate" => $"请用{language}（{framework}框架）编写以下功能的代码：\n{input}\n\n要求：\n- 代码清晰易读\n- 包含必要的注释\n- 遵循最佳实践\n- 包含错误处理",

                "review" => $"请审查以下{language}代码，指出问题和改进建议：\n{input}\n\n请检查：\n- 代码质量和可读性\n- 潜在的bug\n- 性能问题\n- 安全漏洞\n- 最佳实践遵循情况",

                "optimize" => $"请优化以下{language}代码，提高性能和可读性：\n{input}\n\n请说明：\n- 做了哪些优化\n- 为什么这样优化\n- 优化后的效果",

                "explain" => $"请详细解释以下{language}代码的功能和逻辑：\n{input}\n\n请说明：\n- 代码的整体功能\n- 关键逻辑的执行流程\n- 使用的设计模式（如有）\n- 可能的改进点",

                "fix" => $"请修复以下{language}代码中的错误：\n{input}\n\n请说明：\n- 发现了什么错误\n- 如何修复的\n- 如何避免类似错误",

                _ => $"请协助处理以下{language}代码相关任务：\n{input}"
            };
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
