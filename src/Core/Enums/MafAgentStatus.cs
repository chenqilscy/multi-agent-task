namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// Agent状态枚举
    /// </summary>
    public enum MafAgentStatus
    {
        /// <summary>正在初始化</summary>
        Initializing,
        /// <summary>空闲，等待任务</summary>
        Idle,
        /// <summary>繁忙，正在处理任务</summary>
        Busy,
        /// <summary>已暂停</summary>
        Suspended,
        /// <summary>发生错误</summary>
        Error,
        /// <summary>已关闭</summary>
        Shutdown
    }
}
