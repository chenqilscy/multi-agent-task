using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using CKY.MultiAgentFramework.Demos.SmartHome.Models;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<SmartHomeControlService> _logger;

        public SmartHomeControlService(
            LightingAgent lightingAgent,
            ClimateAgent climateAgent,
            MusicAgent musicAgent,
            ILogger<SmartHomeControlService> logger)
        {
            _lightingAgent = lightingAgent ?? throw new ArgumentNullException(nameof(lightingAgent));
            _climateAgent = climateAgent ?? throw new ArgumentNullException(nameof(climateAgent));
            _musicAgent = musicAgent ?? throw new ArgumentNullException(nameof(musicAgent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理用户语音命令
        /// </summary>
        public async Task<SmartHomeResponse> ProcessCommandAsync(string command, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("处理命令: {Command}", command);

                // 创建任务请求
                var request = new MafTaskRequest
                {
                    TaskId = Guid.NewGuid().ToString(),
                    UserInput = command,
                    Parameters = new Dictionary<string, object>()
                };

                // 根据命令类型路由到不同的Agent
                MafTaskResponse? response = null;

                if (ContainsKeywords(command, new[] { "灯", "照明", "亮度", "调亮", "调暗" }))
                {
                    response = await _lightingAgent.ExecuteBusinessLogicAsync(request, ct);
                }
                else if (ContainsKeywords(command, new[] { "空调", "温度", "制热", "制冷", "度" }))
                {
                    response = await _climateAgent.ExecuteBusinessLogicAsync(request, ct);
                }
                else if (ContainsKeywords(command, new[] { "音乐", "播放", "暂停", "歌曲" }))
                {
                    response = await _musicAgent.ExecuteBusinessLogicAsync(request, ct);
                }
                else
                {
                    return new SmartHomeResponse
                    {
                        Success = false,
                        Result = "抱歉，我无法理解您的命令。请尝试使用照明、气候或音乐相关的控制指令。"
                    };
                }

                return new SmartHomeResponse
                {
                    Success = response.Success,
                    Result = response.Result ?? string.Empty,
                    Error = response.Error
                };
            }
            catch (Exception ex)
            {
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
        /// 检查命令是否包含指定关键词
        /// </summary>
        private static bool ContainsKeywords(string command, string[] keywords)
        {
            return keywords.Any(keyword => command.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取所有设备状态
        /// </summary>
        public async Task<Dictionary<string, List<DeviceStatus>>> GetAllDeviceStatusAsync(CancellationToken ct = default)
        {
            // TODO: 实现从各个Agent获取设备状态的逻辑
            await Task.CompletedTask;
            return new Dictionary<string, List<DeviceStatus>>();
        }
    }
}
