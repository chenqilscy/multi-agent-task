namespace CKY.MultiAgentFramework.Services.RealTime
{
    /// <summary>
    /// 实时通知服务
    /// 封装 SignalR Hub 上下文，用于从服务器端向客户端推送消息
    /// </summary>
    public interface IRealTimeNotificationService
    {
        /// <summary>
        /// 向所有客户端发送消息
        /// </summary>
        Task SendBroadcastAsync(string user, string message);

        /// <summary>
        /// 向特定客户端发送消息
        /// </summary>
        Task SendToClientAsync(string connectionId, string user, string message);

        /// <summary>
        /// 向房间发送消息
        /// </summary>
        Task SendToRoomAsync(string roomName, string user, string message);

        /// <summary>
        /// 发送命令执行结果
        /// </summary>
        Task SendCommandResultAsync(string commandId, bool success, string result);

        /// <summary>
        /// 发送设备状态更新
        /// </summary>
        Task SendDeviceUpdateAsync(string deviceId, object status);

        /// <summary>
        /// 发送任务进度更新
        /// </summary>
        Task SendTaskProgressAsync(string taskId, int progress, string status);

        /// <summary>
        /// 发送系统通知
        /// </summary>
        Task SendSystemNotificationAsync(string title, string message, string level = "info");
    }
}
