using CKY.MultiAgentFramework.Services.RealTime;
using CKY.MAF.Demos.SmartHome.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Services
{
    /// <summary>
    /// 实时通知服务实现
    /// 封装 SignalR Hub 上下文，用于从服务器端向客户端推送消息
    /// </summary>
    public class RealTimeNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<MafHub> _hubContext;
        private readonly ILogger<RealTimeNotificationService> _logger;

        public RealTimeNotificationService(
            IHubContext<MafHub> hubContext,
            ILogger<RealTimeNotificationService> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 向所有客户端发送消息
        /// </summary>
        public async Task SendBroadcastAsync(string user, string message)
        {
            _logger.LogInformation("广播消息: {User} - {Message}", user, message);
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// 向特定客户端发送消息
        /// </summary>
        public async Task SendToClientAsync(string connectionId, string user, string message)
        {
            _logger.LogInformation("发送消息到客户端 {ConnectionId}: {User} - {Message}",
                connectionId, user, message);
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// 向房间发送消息
        /// </summary>
        public async Task SendToRoomAsync(string roomName, string user, string message)
        {
            _logger.LogInformation("发送消息到房间 {RoomName}: {User} - {Message}",
                roomName, user, message);
            await _hubContext.Clients.Group(roomName).SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// 发送命令执行结果
        /// </summary>
        public async Task SendCommandResultAsync(string commandId, bool success, string result)
        {
            _logger.LogInformation("发送命令结果: {CommandId} - {Success}", commandId, success);
            await _hubContext.Clients.All.SendAsync("ReceiveCommandResult", commandId, success, result);
        }

        /// <summary>
        /// 发送设备状态更新
        /// </summary>
        public async Task SendDeviceUpdateAsync(string deviceId, object status)
        {
            _logger.LogDebug("发送设备状态更新: {DeviceId}", deviceId);
            await _hubContext.Clients.Group($"device_{deviceId}")
                .SendAsync("ReceiveDeviceUpdate", deviceId, status);
        }

        /// <summary>
        /// 发送任务进度更新
        /// </summary>
        public async Task SendTaskProgressAsync(string taskId, int progress, string status)
        {
            _logger.LogDebug("发送任务进度: {TaskId} - {Progress}%", taskId, progress);
            await _hubContext.Clients.All.SendAsync("ReceiveTaskProgress", taskId, progress, status);
        }

        /// <summary>
        /// 发送系统通知
        /// </summary>
        public async Task SendSystemNotificationAsync(string title, string message, string level = "info")
        {
            _logger.LogInformation("发送系统通知: {Title} - {Level}", title, level);
            await _hubContext.Clients.All.SendAsync("ReceiveSystemNotification", title, message, level);
        }
    }
}
