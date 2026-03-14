// MAF SignalR 客户端封装
class MafSignalRClient {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 3000;
        this.eventHandlers = new Map();
    }

    /**
     * 建立 SignalR 连接
     * @param {string} hubUrl - Hub 的 URL
     */
    async connect(hubUrl = '/hub/maf') {
        try {
            // 动态导入 SignalR JavaScript 客户端
            const signalR = await import('@microsoft/signalr');

            // 创建连接构建器
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(hubUrl)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        // 自定义重试延迟逻辑
                        if (retryContext.previousRetryCount === 0) {
                            return 0;
                        }
                        return Math.min(this.reconnectDelay * Math.pow(2, retryContext.previousRetryCount - 1), 30000);
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // 注册连接状态事件
            this.registerConnectionEvents();

            // 启动连接
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;

            console.log('SignalR 连接已建立');
            this.trigger('connected', {});

        } catch (error) {
            console.error('SignalR 连接失败:', error);
            this.isConnected = false;
            this.trigger('error', { error });
            throw error;
        }
    }

    /**
     * 断开 SignalR 连接
     */
    async disconnect() {
        if (this.connection) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                console.log('SignalR 连接已断开');
                this.trigger('disconnected', {});
            } catch (error) {
                console.error('断开 SignalR 连接时出错:', error);
            }
        }
    }

    /**
     * 注册连接状态事件
     */
    registerConnectionEvents() {
        if (!this.connection) return;

        // 连接关闭事件
        this.connection.onclose(error => {
            this.isConnected = false;
            if (error) {
                console.error('SignalR 连接异常关闭:', error);
                this.trigger('error', { error });
            } else {
                console.log('SignalR 连接已关闭');
                this.trigger('disconnected', {});
            }
        });

        // 重连事件
        this.connection.onreconnecting(error => {
            console.log('SignalR 正在重连...', error);
            this.trigger('reconnecting', { error });
        });

        // 重连成功事件
        this.connection.onreconnected(connectionId => {
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log('SignalR 重连成功:', connectionId);
            this.trigger('reconnected', { connectionId });
        });
    }

    /**
     * 注册服务器方法事件处理器
     * @param {string} methodName - 方法名
     * @param {Function} handler - 事件处理函数
     */
    on(methodName, handler) {
        if (!this.connection) {
            throw new Error('SignalR 连接未建立');
        }

        this.connection.on(methodName, handler);
        this.eventHandlers.set(methodName, handler);
    }

    /**
     * 移除事件处理器
     * @param {string} methodName - 方法名
     */
    off(methodName) {
        if (!this.connection) return;

        this.connection.off(methodName);
        this.eventHandlers.delete(methodName);
    }

    /**
     * 调用服务器方法
     * @param {string} methodName - 方法名
     * @param {...any} args - 方法参数
     */
    async invoke(methodName, ...args) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR 连接未建立或已断开');
        }

        try {
            return await this.connection.invoke(methodName, ...args);
        } catch (error) {
            console.error(`调用服务器方法 ${methodName} 失败:`, error);
            throw error;
        }
    }

    /**
     * 发送消息
     * @param {string} user - 用户名
     * @param {string} message - 消息内容
     */
    async sendMessage(user, message) {
        await this.invoke('SendMessage', user, message);
    }

    /**
     * 加入房间
     * @param {string} roomName - 房间名
     */
    async joinRoom(roomName) {
        await this.invoke('JoinRoom', roomName);
    }

    /**
     * 离开房间
     * @param {string} roomName - 房间名
     */
    async leaveRoom(roomName) {
        await this.invoke('LeaveRoom', roomName);
    }

    /**
     * 订阅设备更新
     * @param {string} deviceId - 设备ID
     */
    async subscribeToDeviceUpdates(deviceId) {
        await this.invoke('SubscribeToDeviceUpdates', deviceId);
    }

    /**
     * 取消订阅设备更新
     * @param {string} deviceId - 设备ID
     */
    async unsubscribeFromDeviceUpdates(deviceId) {
        await this.invoke('UnsubscribeFromDeviceUpdates', deviceId);
    }

    /**
     * 触发自定义事件
     * @param {string} eventName - 事件名
     * @param {any} data - 事件数据
     */
    trigger(eventName, data) {
        const handler = this.eventHandlers.get(`on${eventName}`);
        if (handler) {
            handler(data);
        }
    }

    /**
     * 获取连接状态
     */
    getConnectionState() {
        return {
            isConnected: this.isConnected,
            reconnectAttempts: this.reconnectAttempts
        };
    }
}

// 创建全局单例实例
window.mafSignalR = new MafSignalRClient();

// 自动连接（可选，可以根据需求调整）
document.addEventListener('DOMContentLoaded', async () => {
    try {
        await window.mafSignalR.connect();

        // 注册默认的消息接收事件
        window.mafSignalR.on('ReceiveMessage', (user, message) => {
            console.log(`收到消息 [${user}]: ${message}`);
            // 可以在这里触发自定义事件或更新UI
            const event = new CustomEvent('maf-message', {
                detail: { user, message }
            });
            window.dispatchEvent(event);
        });

        // 注册命令结果事件
        window.mafSignalR.on('ReceiveCommandResult', (commandId, success, result) => {
            console.log(`命令结果 [${commandId}]: ${success ? '成功' : '失败'} - ${result}`);
            const event = new CustomEvent('maf-command-result', {
                detail: { commandId, success, result }
            });
            window.dispatchEvent(event);
        });

        // 注册设备状态更新事件
        window.mafSignalR.on('ReceiveDeviceUpdate', (deviceId, status) => {
            console.log(`设备更新 [${deviceId}]:`, status);
            const event = new CustomEvent('maf-device-update', {
                detail: { deviceId, status }
            });
            window.dispatchEvent(event);
        });

        // 注册系统通知事件
        window.mafSignalR.on('ReceiveSystemNotification', (title, message, level) => {
            console.log(`系统通知 [${level}]: ${title} - ${message}`);
            const event = new CustomEvent('maf-system-notification', {
                detail: { title, message, level }
            });
            window.dispatchEvent(event);
        });

    } catch (error) {
        console.error('自动连接 SignalR 失败:', error);
    }
});

// 导出到全局作用域
window.MafSignalRClient = MafSignalRClient;
