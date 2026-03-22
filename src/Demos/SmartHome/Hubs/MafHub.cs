using Microsoft.AspNetCore.SignalR;

namespace CKY.MAF.Demos.SmartHome.Hubs
{
    /// <summary>
    /// MAF 实时通信 Hub
    /// 提供客户端与服务器的实时双向通信
    /// </summary>
    public class MafHub : Hub
    {
        private readonly ILogger<MafHub> _logger;

        public MafHub(ILogger<MafHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 客户端连接时调用
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("客户端已连接: {ConnectionId}", connectionId);

            // 发送欢迎消息
            await Clients.Caller.SendAsync("ReceiveMessage", "系统", "欢迎连接到 CKY.MAF 实时通信服务");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 客户端断开连接时调用
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (exception != null)
            {
                _logger.LogWarning(exception, "客户端异常断开: {ConnectionId}", connectionId);
            }
            else
            {
                _logger.LogInformation("客户端正常断开: {ConnectionId}", connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 客户端发送消息
        /// </summary>
        public async Task SendMessage(string user, string message)
        {
            // 输入验证
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(message))
                return;
            if (user.Length > 100) user = user[..100];
            if (message.Length > 2000) message = message[..2000];

            _logger.LogInformation("收到消息: {User} - {MessageLength}字符", user, message.Length);

            // 广播消息给所有客户端
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// 加入房间（用于多用户协作场景）
        /// </summary>
        public async Task JoinRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName) || roomName.Length > 100)
                return;

            var connectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(connectionId, roomName);
            _logger.LogInformation("客户端 {ConnectionId} 加入房间: {RoomName}", connectionId, roomName);

            await Clients.Group(roomName).SendAsync("ReceiveMessage", "系统",
                $"新用户加入了房间");
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public async Task LeaveRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName) || roomName.Length > 100)
                return;

            var connectionId = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connectionId, roomName);
            _logger.LogInformation("客户端 {ConnectionId} 离开房间: {RoomName}", connectionId, roomName);

            await Clients.Group(roomName).SendAsync("ReceiveMessage", "系统",
                $"用户离开了房间");
        }

        /// <summary>
        /// 发送命令执行结果
        /// </summary>
        public async Task SendCommandResult(string commandId, bool success, string result)
        {
            await Clients.All.SendAsync("ReceiveCommandResult", commandId, success, result);
            _logger.LogInformation("命令结果已广播: {CommandId} - {Success}", commandId, success);
        }

        /// <summary>
        /// 订阅设备状态更新
        /// </summary>
        public async Task SubscribeToDeviceUpdates(string deviceId)
        {
            var connectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(connectionId, $"device_{deviceId}");
            _logger.LogInformation("客户端 {ConnectionId} 订阅设备更新: {DeviceId}", connectionId, deviceId);
        }

        /// <summary>
        /// 取消订阅设备状态更新
        /// </summary>
        public async Task UnsubscribeFromDeviceUpdates(string deviceId)
        {
            var connectionId = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connectionId, $"device_{deviceId}");
            _logger.LogInformation("客户端 {ConnectionId} 取消订阅设备更新: {DeviceId}", connectionId, deviceId);
        }
    }
}
