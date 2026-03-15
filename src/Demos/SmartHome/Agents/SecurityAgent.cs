using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Agents
{
    /// <summary>
    /// 安防控制Agent
    /// 处理门锁、摄像头、安全模式和告警相关命令
    /// </summary>
    public class SecurityAgent : MafBusinessAgentBase
    {
        private readonly ISecurityService _securityService;

        public override string AgentId => "security-agent-001";
        public override string Name => "SecurityAgent";
        public override string Description => "智能安防控制Agent，支持门锁控制、摄像头管理、外出安防模式和入侵检测";
        public override IReadOnlyList<string> Capabilities =>
            ["security", "door-lock", "camera", "away-mode", "intrusion-detection"];

        public SecurityAgent(
            ISecurityService securityService,
            IMafAiAgentRegistry llmRegistry,
            ILogger<SecurityAgent> logger)
            : base(llmRegistry, logger)
        {
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            Logger.LogInformation("SecurityAgent processing: {UserInput}", request.UserInput);

            var userInput = request.UserInput;

            // 门锁控制
            if (userInput.Contains("锁门") || userInput.Contains("上锁") || userInput.Contains("锁上"))
            {
                return await HandleLockDoorAsync(request, ct);
            }

            if (userInput.Contains("开门") || userInput.Contains("解锁") || userInput.Contains("开锁"))
            {
                return await HandleUnlockDoorAsync(request, ct);
            }

            // 摄像头控制
            if (userInput.Contains("摄像头") || userInput.Contains("监控"))
            {
                if (userInput.Contains("打开") || userInput.Contains("启动") || userInput.Contains("开启"))
                    return await HandleCameraControlAsync(request, true, ct);
                if (userInput.Contains("关闭") || userInput.Contains("停止"))
                    return await HandleCameraControlAsync(request, false, ct);
                return await HandleCameraStatusAsync(request, ct);
            }

            // 外出安防模式
            if (userInput.Contains("外出模式") || userInput.Contains("离家模式") || userInput.Contains("外出安防"))
            {
                if (userInput.Contains("关闭") || userInput.Contains("取消"))
                    return await HandleDisableAwayModeAsync(request, ct);
                return await HandleEnableAwayModeAsync(request, ct);
            }

            // 模拟有人在家
            if (userInput.Contains("模拟有人") || userInput.Contains("模拟在家"))
            {
                if (userInput.Contains("关闭") || userInput.Contains("取消"))
                    return await HandleDisablePresenceSimulationAsync(request, ct);
                return await HandleEnablePresenceSimulationAsync(request, ct);
            }

            // 查看门锁状态
            if (userInput.Contains("门锁") || userInput.Contains("门") && userInput.Contains("状态"))
            {
                return await HandleDoorStatusAsync(request, ct);
            }

            // 查看安防警报
            if (userInput.Contains("警报") || userInput.Contains("告警") || userInput.Contains("异常"))
            {
                return await HandleAlertsQueryAsync(request, ct);
            }

            // 兜底：显示安防功能菜单
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = "🔐 安防控制功能：\n" +
                         "• 门锁控制：「锁门」/「解锁」\n" +
                         "• 摄像头管理：「打开/关闭摄像头」\n" +
                         "• 外出安防：「开启外出模式」\n" +
                         "• 模拟在家：「开启模拟有人在家」\n" +
                         "• 查看状态：「门锁状态」/「摄像头状态」\n" +
                         "• 查看警报：「查看最近警报」",
            };
        }

        private async Task<MafTaskResponse> HandleLockDoorAsync(MafTaskRequest request, CancellationToken ct)
        {
            var location = ExtractLocation(request.UserInput);
            try
            {
                var success = await _securityService.LockDoorAsync(location, ct);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = success,
                    Result = success
                        ? $"🔒 {location}已锁好。"
                        : $"❌ {location}门锁操作失败，请检查设备状态。",
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to lock door at {Location}", location);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Result = $"⚠️ {location}门锁设备离线，请手动确认门锁状态。",
                    Error = ex.Message,
                };
            }
        }

        private async Task<MafTaskResponse> HandleUnlockDoorAsync(MafTaskRequest request, CancellationToken ct)
        {
            var location = ExtractLocation(request.UserInput);
            try
            {
                var success = await _securityService.UnlockDoorAsync(location, ct);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = success,
                    Result = success
                        ? $"🔓 {location}已解锁。"
                        : $"❌ {location}解锁失败。",
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to unlock door at {Location}", location);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Result = $"⚠️ {location}门锁设备离线，请手动操作。",
                    Error = ex.Message,
                };
            }
        }

        private async Task<MafTaskResponse> HandleCameraControlAsync(
            MafTaskRequest request, bool active, CancellationToken ct)
        {
            var location = ExtractCameraLocation(request.UserInput);
            var success = await _securityService.SetCameraActiveAsync(location, active, ct);
            var action = active ? "已启动" : "已关闭";
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = success,
                Result = success
                    ? $"📹 {location}摄像头{action}。"
                    : $"❌ 未找到{location}的摄像头设备。",
            };
        }

        private async Task<MafTaskResponse> HandleCameraStatusAsync(MafTaskRequest request, CancellationToken ct)
        {
            var cameras = await _securityService.GetCameraStatusListAsync(ct);
            var lines = cameras.Select(c =>
                $"• {c.Location}：{(c.IsActive ? "✅ 运行中" : "⭕ 未启动")}" +
                $"{(c.HasMotionDetection ? " (移动侦测)" : "")}");

            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = "📹 摄像头状态：\n" + string.Join("\n", lines),
                Data = cameras,
            };
        }

        private async Task<MafTaskResponse> HandleDoorStatusAsync(MafTaskRequest request, CancellationToken ct)
        {
            var location = ExtractLocation(request.UserInput);
            var status = await _securityService.GetDoorLockStatusAsync(location, ct);
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = $"🚪 {status.Location}：{(status.IsLocked ? "🔒 已锁定" : "🔓 未锁定")}" +
                         $"\n• 最后变更：{status.LastChanged:HH:mm:ss}",
                Data = status,
            };
        }

        private async Task<MafTaskResponse> HandleEnableAwayModeAsync(MafTaskRequest request, CancellationToken ct)
        {
            try
            {
                var success = await _securityService.EnableAwayModeAsync(ct);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = success,
                    Result = success
                        ? "🔐 外出安防模式已开启：\n• 所有门锁已锁定\n• 所有摄像头已启动\n• 移动侦测已启用"
                        : "❌ 外出模式启动失败，请检查设备。",
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to enable away mode");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Result = "⚠️ 外出安防模式启动异常，部分设备可能未正常响应。",
                    Error = ex.Message,
                };
            }
        }

        private async Task<MafTaskResponse> HandleDisableAwayModeAsync(MafTaskRequest request, CancellationToken ct)
        {
            var success = await _securityService.DisableAwayModeAsync(ct);
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = success,
                Result = success ? "🏠 外出安防模式已关闭，欢迎回家。" : "❌ 外出模式关闭失败。",
            };
        }

        private async Task<MafTaskResponse> HandleEnablePresenceSimulationAsync(
            MafTaskRequest request, CancellationToken ct)
        {
            var success = await _securityService.EnablePresenceSimulationAsync(ct);
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = success,
                Result = success ? "🏠 模拟有人在家已开启（灯光将随机开关）。" : "❌ 模拟在家功能启动失败。",
            };
        }

        private async Task<MafTaskResponse> HandleDisablePresenceSimulationAsync(
            MafTaskRequest request, CancellationToken ct)
        {
            var success = await _securityService.DisablePresenceSimulationAsync(ct);
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = success,
                Result = success ? "模拟有人在家已关闭。" : "❌ 关闭失败。",
            };
        }

        private async Task<MafTaskResponse> HandleAlertsQueryAsync(MafTaskRequest request, CancellationToken ct)
        {
            var alerts = await _securityService.GetRecentAlertsAsync(5, ct);
            if (alerts.Count == 0)
            {
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = "✅ 最近没有安防警报，一切正常。",
                };
            }

            var lines = alerts.Select(a =>
                $"• [{a.Severity}] {a.Type} - {a.Location}（{a.Timestamp:MM-dd HH:mm}）\n  {a.Description}");

            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = "🚨 最近的安防警报：\n" + string.Join("\n", lines),
                Data = alerts,
            };
        }

        private static string ExtractLocation(string input)
        {
            if (input.Contains("后门")) return "后门";
            if (input.Contains("车库")) return "车库";
            if (input.Contains("阳台")) return "阳台";
            return "大门";
        }

        private static string ExtractCameraLocation(string input)
        {
            if (input.Contains("客厅")) return "客厅";
            if (input.Contains("车库")) return "车库";
            if (input.Contains("门口")) return "门口";
            return "门口";
        }
    }
}
