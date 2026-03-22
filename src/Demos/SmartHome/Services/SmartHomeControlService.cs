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
        private readonly KnowledgeBaseAgent _knowledgeBaseAgent;
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
            KnowledgeBaseAgent knowledgeBaseAgent,
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
            _knowledgeBaseAgent = knowledgeBaseAgent ?? throw new ArgumentNullException(nameof(knowledgeBaseAgent));
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

                // 场景模式：多Agent联动
                if (ContainsKeywords(command, ["睡眠模式", "睡觉模式", "晚安", "夜间模式", "睡眠准备"]))
                {
                    routedAgent = "SceneMode:Sleep";
                    response = await ExecuteSceneModeAsync(routedAgent, "sleep", request, ct);
                }
                else if (ContainsKeywords(command, ["会客模式", "来客人", "朋友来", "派对模式", "聚会模式", "商务洽谈"]))
                {
                    routedAgent = "SceneMode:Guest";
                    response = await ExecuteSceneModeAsync(routedAgent, "guest", request, ct);
                }
                else if (ContainsKeywords(command, ["阅读模式", "看书模式", "专注阅读"]))
                {
                    routedAgent = "SceneMode:Reading";
                    response = await ExecuteSceneModeAsync(routedAgent, "reading", request, ct);
                }
                else if (ContainsKeywords(command, ["电影模式", "观影模式", "看电影"]))
                {
                    routedAgent = "SceneMode:Movie";
                    response = await ExecuteSceneModeAsync(routedAgent, "movie", request, ct);
                }
                else if (ContainsKeywords(command, ["健身模式", "运动模式", "锻炼模式"]))
                {
                    routedAgent = "SceneMode:Exercise";
                    response = await ExecuteSceneModeAsync(routedAgent, "exercise", request, ct);
                }
                else if (ContainsKeywords(command, ["工作模式", "学习模式", "办公模式", "专注模式"]))
                {
                    routedAgent = "SceneMode:Work";
                    response = await ExecuteSceneModeAsync(routedAgent, "work", request, ct);
                }
                else if (ContainsKeywords(command, ["聚餐模式", "晚餐模式", "用餐模式", "聚餐准备"]))
                {
                    routedAgent = "SceneMode:Dinner";
                    response = await ExecuteSceneModeAsync(routedAgent, "dinner", request, ct);
                }
                else if (ContainsKeywords(command, ["天气", "下雨", "气温", "晴天", "预报", "穿什么"]))
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
                else if (ContainsKeywords(command, ["知识库", "帮助", "FAQ", "怎么用", "使用说明", "功能介绍", "如何"]))
                {
                    routedAgent = "KnowledgeBaseAgent";
                    response = await ExecuteAgentWithTracingAsync(_knowledgeBaseAgent, routedAgent, request, ct);
                }
                else
                {
                    activity?.SetTag("routing.result", "unrecognized");
                    return new SmartHomeResponse
                    {
                        Success = false,
                        Result = "抱歉，我无法理解您的命令。请尝试使用照明、气候、音乐、安防、天气查询或知识库相关的指令。"
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
        /// 执行场景模式：协调多个Agent完成联动操作
        /// </summary>
        private async Task<MafTaskResponse> ExecuteSceneModeAsync(
            string sceneLabel,
            string sceneType,
            MafTaskRequest request,
            CancellationToken ct)
        {
            using var childActivity = MafActivitySource.Agent.StartActivity($"smarthome.scene.{sceneType}");
            childActivity?.SetTag("scene.type", sceneType);

            var results = new List<string>();
            var state = new SceneModeState();

            try
            {
                switch (sceneType)
                {
                    case "sleep":
                        // 睡眠模式：调暗灯光 → 适温 → 停止音乐 → 锁门
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_lightingAgent, "LightingAgent",
                                CreateSubRequest(request, "调暗灯光"), ct),
                            "💡 灯光已调暗至30%", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_climateAgent, "ClimateAgent",
                                CreateSubRequest(request, "空调设置22度", new() { ["room"] = "卧室" }), ct),
                            "🌡️ 空调已调至22°C", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_musicAgent, "MusicAgent",
                                CreateSubRequest(request, "暂停音乐"), ct),
                            "🎵 音乐已暂停", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_securityAgent, "SecurityAgent",
                                CreateSubRequest(request, "锁门"), ct),
                            "🔒 门锁已上锁", ct);
                        break;

                    case "guest":
                        // 会客模式：灯光调亮 → 舒适温度 → 播放背景音乐
                        var isParty = request.UserInput.Contains("派对") || request.UserInput.Contains("聚会");
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_lightingAgent, "LightingAgent",
                                CreateSubRequest(request, isParty ? "调亮灯光" : "调亮灯光"), ct),
                            "💡 灯光已调亮", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_climateAgent, "ClimateAgent",
                                CreateSubRequest(request, "空调设置24度", new() { ["room"] = "客厅" }), ct),
                            "🌡️ 空调已调至舒适温度24°C", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_musicAgent, "MusicAgent",
                                CreateSubRequest(request, "播放轻音乐"), ct),
                            "🎵 背景音乐已播放", ct);
                        break;

                    case "reading":
                        // 阅读模式：暖光调暗 → 静音
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_lightingAgent, "LightingAgent",
                                CreateSubRequest(request, "灯光调暗", new() { ["room"] = "书房" }), ct),
                            "💡 书房灯光已调至柔和阅读亮度", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_musicAgent, "MusicAgent",
                                CreateSubRequest(request, "暂停音乐"), ct),
                            "🔇 已静音", ct);
                        break;

                    case "movie":
                        // 电影模式：关灯 → 停止音乐
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_lightingAgent, "LightingAgent",
                                CreateSubRequest(request, "关灯", new() { ["room"] = "客厅" }), ct),
                            "💡 客厅灯光已关闭", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_musicAgent, "MusicAgent",
                                CreateSubRequest(request, "暂停音乐"), ct),
                            "🔇 音乐已暂停", ct);
                        break;

                    case "exercise":
                        // 运动模式：灯光调亮 → 播放动感音乐
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_lightingAgent, "LightingAgent",
                                CreateSubRequest(request, "调亮灯光"), ct),
                            "💡 灯光已调至最亮", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_musicAgent, "MusicAgent",
                                CreateSubRequest(request, "播放音乐"), ct),
                            "🎵 动感音乐已播放", ct);
                        break;

                    case "work":
                        // 工作模式：适中灯光 → 静音 → 适温
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_lightingAgent, "LightingAgent",
                                CreateSubRequest(request, "灯光调亮", new() { ["room"] = "书房" }), ct),
                            "💡 书房灯光已调至工作亮度", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_musicAgent, "MusicAgent",
                                CreateSubRequest(request, "暂停音乐"), ct),
                            "🔇 已静音", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_climateAgent, "ClimateAgent",
                                CreateSubRequest(request, "空调设置24度", new() { ["room"] = "书房" }), ct),
                            "🌡️ 空调已调至24°C", ct);
                        break;

                    case "dinner":
                        // 聚餐模式：暖光 → 背景音乐 → 舒适温度
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_lightingAgent, "LightingAgent",
                                CreateSubRequest(request, "调亮灯光", new() { ["room"] = "餐厅" }), ct),
                            "💡 餐厅灯光已调亮", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_musicAgent, "MusicAgent",
                                CreateSubRequest(request, "播放轻音乐"), ct),
                            "🎵 背景音乐已播放", ct);
                        await ExecuteSubAgentSafe(results, state,
                            () => ExecuteAgentWithTracingAsync(_climateAgent, "ClimateAgent",
                                CreateSubRequest(request, "空调设置25度", new() { ["room"] = "餐厅" }), ct),
                            "🌡️ 空调已调至25°C", ct);
                        break;
                }

                var sceneName = sceneType switch
                {
                    "sleep" => "🌙 睡眠模式",
                    "guest" => "🤝 会客模式",
                    "reading" => "📖 阅读专注模式",
                    "movie" => "🎬 电影观影模式",
                    "exercise" => "🏃 运动健身模式",
                    "work" => "💼 工作学习模式",
                    "dinner" => "🍽️ 聚餐模式",
                    _ => "场景模式"
                };

                return new MafTaskResponse
                {
                    Success = state.AllSuccess,
                    Result = $"{sceneName}已启动：\n" + string.Join("\n", results),
                    Error = state.AllSuccess ? null : "部分设备操作失败",
                };
            }
            catch (Exception ex)
            {
                childActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "场景模式执行出错: {Scene}", sceneType);
                return new MafTaskResponse
                {
                    Success = false,
                    Result = $"场景模式 {sceneType} 执行出错：{ex.Message}",
                    Error = ex.Message,
                };
            }
        }

        /// <summary>
        /// 安全执行子Agent，失败时记录但不中断整个场景
        /// </summary>
        private async Task ExecuteSubAgentSafe(
            List<string> results,
            SceneModeState state,
            Func<Task<MafTaskResponse>> agentAction,
            string successMessage,
            CancellationToken ct)
        {
            try
            {
                var response = await agentAction();
                if (response.Success)
                {
                    results.Add($"✅ {successMessage}");
                }
                else
                {
                    results.Add($"⚠️ {successMessage}（操作异常: {response.Error ?? "未知错误"}）");
                    state.AllSuccess = false;
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ {successMessage}（设备异常: {ex.Message}）");
                state.AllSuccess = false;
                _logger.LogWarning(ex, "场景子Agent执行失败");
            }
        }

        /// <summary>场景模式执行状态</summary>
        private sealed class SceneModeState
        {
            public bool AllSuccess { get; set; } = true;
        }

        /// <summary>
        /// 为场景模式创建子请求
        /// </summary>
        private static MafTaskRequest CreateSubRequest(
            MafTaskRequest parentRequest,
            string subCommand,
            Dictionary<string, object>? additionalParams = null)
        {
            var subRequest = new MafTaskRequest
            {
                TaskId = Guid.NewGuid().ToString(),
                UserInput = subCommand,
                Parameters = new Dictionary<string, object>(parentRequest.Parameters),
            };
            if (additionalParams != null)
            {
                foreach (var kvp in additionalParams)
                    subRequest.Parameters[kvp.Key] = kvp.Value;
            }
            return subRequest;
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
