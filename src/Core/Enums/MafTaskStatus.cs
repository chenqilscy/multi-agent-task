namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum MafTaskStatus
    {
        /// <summary>等待执行</summary>
        Pending,
        /// <summary>准备就绪（依赖已满足）</summary>
        Ready,
        /// <summary>已调度</summary>
        Scheduled,
        /// <summary>执行中</summary>
        Running,
        /// <summary>已完成</summary>
        Completed,
        /// <summary>失败</summary>
        Failed,
        /// <summary>已取消</summary>
        Cancelled,
        /// <summary>超时</summary>
        Timeout
    }
}
