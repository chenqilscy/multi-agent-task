# SignalR 实时通信使用指南

## 概述

CKY.MAF 框架集成了 SignalR 实时通信功能，支持服务器与客户端之间的双向实时通信。

## 服务器端配置

### 1. 注册 SignalR 服务

在 `Program.cs` 中添加 SignalR 服务：

```csharp
// 注册 SignalR 服务
builder.Services.AddMafSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveIntervalSeconds = 15;
});
```

### 2. 映射 Hub 端点

```csharp
var app = builder.Build();

// 映射 SignalR Hub
app.MapMafHubs();

// 或者手动映射
app.MapHub<MafHub>("/hub/maf");
```

### 3. 使用实时通知服务

在任何服务中注入 `IRealTimeNotificationService`：

```csharp
public class MyService
{
    private readonly IRealTimeNotificationService _notificationService;

    public MyService(IRealTimeNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task NotifyAsync(string message)
    {
        // 向所有客户端广播消息
        await _notificationService.SendBroadcastAsync("系统", message);

        // 发送系统通知
        await _notificationService.SendSystemNotificationAsync(
            "操作完成",
            message,
            "success"
        );

        // 发送任务进度
        await _notificationService.SendTaskProgressAsync(
            "task-123",
            75,
            "处理中"
        );
    }
}
```

## 客户端使用

### 1. 引入 JavaScript 客户端

在 HTML 页面中引入 SignalR 客户端库和 MAF 客户端封装：

```html
<!-- SignalR 库 -->
<script src="@Assets["_framework/microsoft.signalr.js"]"></script>
<!-- MAF SignalR 客户端封装 -->
<script src="@Assets["js/maf-signalr.js"]"></script>
```

### 2. 监听实时事件

```javascript
// 监听消息事件
window.addEventListener('maf-message', (event) => {
    const { user, message } = event.detail;
    console.log(`收到消息: ${user} - ${message}`);
    // 更新 UI
    updateChatUI(user, message);
});

// 监听命令结果事件
window.addEventListener('maf-command-result', (event) => {
    const { commandId, success, result } = event.detail;
    console.log(`命令结果: ${commandId} - ${success ? '成功' : '失败'}`);
    // 更新命令状态
    updateCommandStatus(commandId, success, result);
});

// 监听设备状态更新
window.addEventListener('maf-device-update', (event) => {
    const { deviceId, status } = event.detail;
    console.log(`设备更新: ${deviceId}`, status);
    // 更新设备状态
    updateDeviceStatus(deviceId, status);
});

// 监听系统通知
window.addEventListener('maf-system-notification', (event) => {
    const { title, message, level } = event.detail;
    // 显示通知
    showNotification(title, message, level);
});
```

### 3. 手动调用服务器方法

```javascript
// 发送消息
await window.mafSignalR.sendMessage('用户名', '消息内容');

// 加入房间
await window.mafSignalR.joinRoom('room-name');

// 离开房间
await window.mafSignalR.leaveRoom('room-name');

// 订阅设备更新
await window.mafSignalR.subscribeToDeviceUpdates('device-id');

// 取消订阅
await window.mafSignalR.unsubscribeFromDeviceUpdates('device-id');
```

## Blazor 组件集成

### 在 Blazor 组件中使用 SignalR

```csharp
@page "/realtime"
@inject IRealTimeNotificationService NotificationService
@implements IDisposable

<div class="realtime-demo">
    <h3>实时通信演示</h3>
    <div>
        <button @onclick="SendBroadcast">广播消息</button>
        <button @onclick="SendNotification">发送通知</button>
        <button @onclick="SendTaskProgress">发送任务进度</button>
    </div>

    <div class="status">
        <p>连接状态: @(isConnected ? "已连接" : "未连接")</p>
        <ul>
            @foreach (var message in messages)
            {
                <li>@message</li>
            }
        </ul>
    </div>
</div>

@code {
    private bool isConnected = false;
    private List<string> messages = new();

    protected override async Task OnInitializedAsync()
    {
        // 模拟连接状态
        isConnected = true;

        // 在实际应用中，可以通过 JavaScript 互操作来监听连接状态
    }

    private async Task SendBroadcast()
    {
        await NotificationService.SendBroadcastAsync("系统", "这是一条广播消息");
        messages.Add($"[广播] 系统消息已发送");
    }

    private async Task SendNotification()
    {
        await NotificationService.SendSystemNotificationAsync(
            "操作完成",
            "您的请求已成功处理",
            "success"
        );
        messages.Add($"[通知] 操作完成通知已发送");
    }

    private async Task SendTaskProgress()
    {
        var taskId = Guid.NewGuid().ToString();
        for (int i = 0; i <= 100; i += 25)
        {
            await NotificationService.SendTaskProgressAsync(
                taskId,
                i,
                $"处理中... {i}%"
            );
            await Task.Delay(500);
        }
        messages.Add($"[任务] 任务进度已发送: {taskId}");
    }

    public void Dispose()
    {
        // 清理资源
    }
}
```

## 高级用法

### 1. 房间分组

用于实现多用户协作场景：

```csharp
// 用户加入协作房间
await notificationService.SendToRoomAsync(
    "collaboration-room-123",
    "系统",
    "用户A 加入了协作"
);

// 向房间发送消息
await HubContext.Clients.Group("collaboration-room-123")
    .SendAsync("ReceiveMessage", "用户A", "大家好");
```

### 2. 设备状态推送

```csharp
// 当设备状态变化时推送更新
public async Task OnDeviceStatusChanged(string deviceId, DeviceStatus newStatus)
{
    await _notificationService.SendDeviceUpdateAsync(deviceId, newStatus);
}
```

### 3. 任务进度实时更新

```csharp
public async Task ExecuteLongRunningTask(string taskId)
{
    for (int i = 0; i <= 100; i += 10)
    {
        // 执行任务逻辑
        await DoWork(i);

        // 推送进度
        await _notificationService.SendTaskProgressAsync(
            taskId,
            i,
            i == 100 ? "完成" : "处理中"
        );
    }
}
```

## 注意事项

1. **连接管理**: SignalR 连接可能会断开，客户端需要处理重连逻辑
2. **安全性**: 确保在 Hub 方法中实现适当的身份验证和授权
3. **性能**: 避免频繁发送大量消息，考虑使用批量或节流
4. **错误处理**: 始终实现适当的错误处理和日志记录
5. **资源清理**: 在组件销毁时正确清理 SignalR 连接

## 故障排除

### 连接失败

1. 检查 SignalR 服务是否正确注册
2. 确认 Hub URL 配置正确
3. 查看浏览器控制台错误信息
4. 验证服务器端日志

### 消息未接收

1. 确认事件处理器已正确注册
2. 检查方法名称是否匹配
3. 验证参数类型和数量
4. 查看网络请求和响应

### 性能问题

1. 减少消息频率
2. 使用批量发送
3. 优化消息大小
4. 考虑使用 WebSocket 而非长轮询
