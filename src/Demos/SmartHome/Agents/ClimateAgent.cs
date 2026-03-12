using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Agents
{
    /// <summary>
    /// 气候控制Agent
    /// 负责处理空调、温度调节等气候控制命令
    /// </summary>
    public class ClimateAgent : MafAgentBase
    {
        private readonly IClimateService _climateService;

        public override string AgentId => "climate-agent-001";
        public override string Name => "ClimateAgent";
        public override string Description => "智能气候控制Agent，支持温度调节、制冷制热等功能";
        public override IReadOnlyList<string> Capabilities => ["climate", "temperature-control", "air-conditioning"];

        public ClimateAgent(
            IClimateService climateService,
            IMafSessionStorage sessionStorage,
            IPriorityCalculator priorityCalculator,
            IMetricsCollector metricsCollector,
            ILogger<ClimateAgent> logger)
            : base(sessionStorage, priorityCalculator, metricsCollector, logger)
        {
            _climateService = climateService ?? throw new ArgumentNullException(nameof(climateService));
        }

        protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            IAgentSession session,
            CancellationToken ct = default)
        {
            var userInput = request.UserInput;
            string room = request.Parameters.TryGetValue("room", out var r) ? r?.ToString() ?? "客厅" : "客厅";

            if (userInput.Contains("冷") || userInput.Contains("制冷"))
            {
                await _climateService.SetModeAsync(room, "cooling", ct);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"{room}空调已切换到制冷模式"
                };
            }
            else if (userInput.Contains("热") || userInput.Contains("制热") || userInput.Contains("暖"))
            {
                await _climateService.SetModeAsync(room, "heating", ct);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"{room}空调已切换到制热模式"
                };
            }
            else if (userInput.Contains("度"))
            {
                // 解析温度值
                var temperature = ExtractTemperature(userInput);
                if (temperature.HasValue)
                {
                    await _climateService.SetTemperatureAsync(room, temperature.Value, ct);
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = true,
                        Result = $"{room}温度已设置为{temperature.Value}度"
                    };
                }
            }

            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Error = "无法识别的气候控制命令"
            };
        }

        private static int? ExtractTemperature(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsDigit(input[i]))
                {
                    var numStr = string.Concat(input.Skip(i).TakeWhile(char.IsDigit));
                    if (int.TryParse(numStr, out var temp) && temp >= 16 && temp <= 30)
                    {
                        return temp;
                    }
                }
            }
            return null;
        }
    }
}
