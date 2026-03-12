namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 任务组执行模式枚举
    /// </summary>
    public enum GroupExecutionMode
    {
        /// <summary>串行执行</summary>
        Serial,
        /// <summary>并行执行</summary>
        Parallel
    }

    /// <summary>
    /// 任务组状态枚举
    /// </summary>
    public enum GroupStatus
    {
        /// <summary>等待执行</summary>
        Pending,
        /// <summary>执行中</summary>
        Running,
        /// <summary>已完成</summary>
        Completed,
        /// <summary>失败</summary>
        Failed,
        /// <summary>已取消</summary>
        Cancelled
    }
}
