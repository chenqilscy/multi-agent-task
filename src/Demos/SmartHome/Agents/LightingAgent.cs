using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Agents
{
    /// <summary>
    /// 照明控制Agent
    /// 负责处理智能家居中与灯光相关的所有控制命令
    /// </summary>
    public class LightingAgent : MafAgentBase
    {
        private readonly ILightingService _lightingService;

        public override string AgentId => "lighting-agent-001";
        public override string Name => "LightingAgent";
        public override string Description => "智能照明控制Agent，支持开关灯、调节亮度和颜色等功能";
        public override IReadOnlyList<string> Capabilities => ["lighting", "light-control", "brightness-control"];

        public LightingAgent(
            ILightingService lightingService,
            IMafSessionStorage sessionStorage,
            IPriorityCalculator priorityCalculator,
            IMetricsCollector metricsCollector,
            ILogger<LightingAgent> logger)
            : base(sessionStorage, priorityCalculator, metricsCollector, logger)
        {
            _lightingService = lightingService ?? throw new ArgumentNullException(nameof(lightingService));
        }

        protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            IAgentSession session,
            CancellationToken ct = default)
        {
            var userInput = request.UserInput;
            string room = request.Parameters.TryGetValue("room", out var r) ? r?.ToString() ?? "客厅" : "客厅";

            // 解析命令
            if (userInput.Contains("打开") || userInput.Contains("开灯"))
            {
                await _lightingService.TurnOnAsync(room, ct);

                // 保存操作到会话上下文
                session.Context["last_lighting_action"] = "turn_on";
                session.Context["last_room"] = room;

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"{room}的灯已打开"
                };
            }
            else if (userInput.Contains("关闭") || userInput.Contains("关灯"))
            {
                await _lightingService.TurnOffAsync(room, ct);

                session.Context["last_lighting_action"] = "turn_off";
                session.Context["last_room"] = room;

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"{room}的灯已关闭"
                };
            }
            else if (userInput.Contains("调暗") || userInput.Contains("暗"))
            {
                await _lightingService.SetBrightnessAsync(room, 30, ct);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"{room}的灯光已调暗（亮度30%）"
                };
            }
            else if (userInput.Contains("调亮") || userInput.Contains("亮"))
            {
                await _lightingService.SetBrightnessAsync(room, 100, ct);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"{room}的灯光已调亮（亮度100%）"
                };
            }

            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Error = "无法识别的照明控制命令",
                Result = "抱歉，我无法理解您的照明控制命令，请尝试说'打开灯'或'关闭灯'"
            };
        }
    }
}
