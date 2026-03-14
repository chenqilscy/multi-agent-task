namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 调度计划状态
    /// </summary>
    public enum SchedulePlanStatus
    {
        /// <summary>已创建</summary>
        Created,

        /// <summary>执行中</summary>
        Running,

        /// <summary>已完成</summary>
        Completed,

        /// <summary>已取消</summary>
        Cancelled,

        /// <summary>失败</summary>
        Failed
    }

    /// <summary>
    /// 执行计划状态
    /// </summary>
    public enum ExecutionPlanStatus
    {
        /// <summary>已创建</summary>
        Created,

        /// <summary>执行中</summary>
        Running,

        /// <summary>已完成</summary>
        Completed,

        /// <summary>部分完成</summary>
        PartiallyCompleted,

        /// <summary>已取消</summary>
        Cancelled,

        /// <summary>失败</summary>
        Failed
    }
}
