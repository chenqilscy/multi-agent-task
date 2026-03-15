using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using CKY.MultiAgentFramework.Demos.SmartHome.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Services
{
    /// <summary>
    /// 智能家居控制服务
    /// 负责协调各个Agent处理用户命令
    /// </summary>
    public class SmartHomeControlService
    {
        private readonly LightingAgent _lightingAgent;
        private readonly ClimateAgent _climateAgent;
        private readonly MusicAgent _musicAgent;
        private readonly WeatherAgent _weatherAgent;
        private readonly TemperatureHistoryAgent _temperatureHistoryAgent;
        private readonly SecurityAgent _securityAgent;
        private readonly IDegradationManager _degradationManager;
        private readonly IRuleEngine _ruleEngine;
        private readonly ILogger<SmartHomeControlService> _logger;

        public SmartHomeControlService(
            LightingAgent lightingAgent,
            ClimateAgent climateAgent,
            MusicAgent musicAgent,
            WeatherAgent weatherAgent,
            TemperatureHistoryAgent temperatureHistoryAgent,
            SecurityAgent securityAgent,
            IDegradationManager degradationManager,
            IRuleEngine ruleEngine,
            ILogger<SmartHomeControlService> logger)
        {
            _lightingAgent = lightingAgent ?? throw new ArgumentNullException(nameof(lightingAgent));
            _climateAgent = climateAgent ?? throw new ArgumentNullException(nameof(climateAgent));
            _musicAgent = musicAgent ?? throw new ArgumentNullException(nameof(musicAgent));
            _weatherAgent = weatherAgent ?? throw new ArgumentNullException(nameof(weatherAgent));
            _temperatureHistoryAgent = temperatureHistoryAgent
                ?? throw new ArgumentNullException(nameof(temperatureHistoryAgent));
            _securityAgent = securityAgent ?? throw new ArgumentNullException(nameof(securityAgent));
            _degradationManager = degradationManager ?? throw new ArgumentNullException(nameof(degradationManager));
            _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理用户语音命令
        /// </summary>
        public async Task<SmartHomeResponse> ProcessCommandAsync(string command, CancellationToken ct = default)
        {
            using var activity = MafActivitySource.Agent.StartActivity("smarthome.process_command");
            activity?.SetTag("command.length", command.Length);

            try
            {
                _logger.LogInformation("处理命令: {Command}", command);

                // Level 5 降级：完全禁用 LLM，使用规则引擎处理
                if (!_degradationManager.IsFeatureEnabled("llm") && _ruleEngine.CanHandle(command))
                {
                    _logger.LogWarning("LLM 已降级至 Level 5，使用规则引擎处理: {Command}", command);
                    activity?.SetTag("routing.degradation", true);
                    activity?.SetTag("routing.degradation_level", _degradationManager.CurrentLevel.ToString());

                    var ruleRequest = new MafTaskRequest
                    {
                        TaskId = Guid.NewGuid().ToString(),
                        UserInput = command,
                        Parameters = new Dictionary<string, object>()
                    };
                    var ruleResponse = await _ruleEngine.ProcessAsync(ruleRequest, ct);

                    activity?.SetTag("routing.agent", "RuleEngine");
                    activity?.SetTag("routing.success", ruleResponse.Success);

                    return new SmartHomeResponse
                    {
                        Success = ruleResponse.Success,
                        Result = ruleResponse.Result ?? string.Empty,
                        Error = ruleResponse.Error,
                    };
                }

                // 创建任务请求
                var request = new MafTaskRequest
                {
                    TaskId = Guid.NewGuid().ToString(),
                    UserInput = command,
                    Parameters = new Dictionary<string, object>()
                };

                // 根据命令类型路由到不同的Agent
                MafTaskResponse? response = null;
                string routedAgent;

                if (ContainsKeywords(command, ["天气", "下雨", "气温", "晴天", "预报", "穿什么"]))
                {
                    routedAgent = "WeatherAgent";
                    ExtractCityEntity(command, request.Parameters);
                    ExtractDateEntity(command, request.Parameters);
                    response = await ExecuteAgentWithTracingAsync(_weatherAgent, routedAgent, request, ct);
                }
                else if (ContainsKeywords(command, ["温度变化", "温度历史", "这段时间温度", "最近温度", "传感器"]))
                {
                    routedAgent = "TemperatureHistoryAgent";
                    ExtractRoomEntity(command, request.Parameters);
                    response = await ExecuteAgentWithTracingAsync(_temperatureHistoryAgent, routedAgent, request, ct);
                }
                else if (ContainsKeywords(command, ["门锁", "锁门", "上锁", "解锁", "开锁", "摄像头", "监控", "外出模式", "离家模式", "外出安防", "模拟有人", "模拟在家", "警报", "告警"]))
                {
                    routedAgent = "SecurityAgent";
                    response = await ExecuteAgentWithTracingAsync(_securityAgent, routedAgent, request, ct);
                }
                else if (ContainsKeywords(command, ["灯", "照明", "亮度", "调亮", "调暗"]))
                {
                    routedAgent = "LightingAgent";
                    ExtractRoomEntity(command, request.Parameters);
                    response = await ExecuteAgentWithTracingAsync(_lightingAgent, routedAgent, request, ct);
                }
                else if (ContainsKeywords(command, ["空调", "温度", "制热", "制冷", "度"]))
                {
                    routedAgent = "ClimateAgent";
                    ExtractRoomEntity(command, request.Parameters);
                    response = await ExecuteAgentWithTracingAsync(_climateAgent, routedAgent, request, ct);
                }
                else if (ContainsKeywords(command, ["音乐", "播放", "暂停", "歌曲"]))
                {
                    routedAgent = "MusicAgent";
                    response = await ExecuteAgentWithTracingAsync(_musicAgent, routedAgent, request, ct);
                }
                else
                {
                    activity?.SetTag("routing.result", "unrecognized");
                    return new SmartHomeResponse
                    {
                        Success = false,
                        Result = "抱歉，我无法理解您的命令。请尝试使用照明、气候、音乐、安防或天气查询相关的指令。"
                    };
                }

                activity?.SetTag("routing.agent", routedAgent);
                activity?.SetTag("routing.success", response.Success);

                return new SmartHomeResponse
                {
                    Success = response.Success,
                    Result = response.Result ?? string.Empty,
                    Error = response.Error,
                    NeedsClarification = response.NeedsClarification,
                    ClarificationQuestion = response.ClarificationQuestion,
                    ClarificationOptions = response.ClarificationOptions,
                };
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "处理命令时发生错误: {Command}", command);
                return new SmartHomeResponse
                {
                    Success = false,
                    Result = "处理命令时发生错误，请稍后重试。",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 带追踪的 Agent 执行
        /// </summary>
        private static async Task<MafTaskResponse> ExecuteAgentWithTracingAsync(
            MafBusinessAgentBase agent,
            string agentName,
            MafTaskRequest request,
            CancellationToken ct)
        {
            using var childActivity = MafActivitySource.Agent.StartActivity($"smarthome.agent.{agentName.ToLowerInvariant()}");
            childActivity?.SetTag("agent.name", agentName);
            childActivity?.SetTag("task.id", request.TaskId);

            var response = await agent.ExecuteBusinessLogicAsync(request, ct);

            childActivity?.SetTag("agent.success", response.Success);
            if (!response.Success && response.Error != null)
                childActivity?.SetTag("agent.error", response.Error);

            return response;
        }

        /// <summary>
        /// 检查命令是否包含指定关键词
        /// </summary>
        private static bool ContainsKeywords(string command, string[] keywords)
        {
            return keywords.Any(keyword => command.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static void ExtractCityEntity(string command, Dictionary<string, object> parameters)
        {
            var cities = new[]
            {
                "北京", "上海", "广州", "深圳", "成都", "杭州", "南京",
                "武汉", "重庆", "西安", "苏州", "天津",
            };
            foreach (var city in cities)
            {
                if (command.Contains(city))
                {
                    parameters["city"] = city;
                    return;
                }
            }
        }

        private static void ExtractDateEntity(string command, Dictionary<string, object> parameters)
        {
            if (command.Contains("明天"))
                parameters["date"] = "明天";
            else if (command.Contains("后天"))
                parameters["date"] = "后天";
            else
                parameters["date"] = "今天";
        }

        private static void ExtractRoomEntity(string command, Dictionary<string, object> parameters)
        {
            var rooms = new[] { "客厅", "卧室", "厨房", "浴室", "书房", "餐厅", "阳台" };
            foreach (var room in rooms)
            {
                if (command.Contains(room))
                {
                    parameters["room"] = room;
                    return;
                }
            }
        }

        /// <summary>
        /// 获取所有设备状态
        /// </summary>
        public async Task<Dictionary<string, List<DeviceStatus>>> GetAllDeviceStatusAsync(CancellationToken ct = default)
        {
            // 从各个 Agent 获取设备状态（需要各 Agent 实现状态查询接口后在此处调用）
            await Task.CompletedTask;
            return new Dictionary<string, List<DeviceStatus>>();
        }
    }
}
