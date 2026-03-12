namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 任务优先级枚举
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>后台任务（日志、统计等）</summary>
        Background = 1,
        /// <summary>低优先级（后台任务）</summary>
        Low = 2,
        /// <summary>普通优先级（常规任务）</summary>
        Normal = 3,
        /// <summary>高优先级（用户明确要求）</summary>
        High = 4,
        /// <summary>关键任务（安全相关、用户强制中断）</summary>
        Critical = 5
    }
}
