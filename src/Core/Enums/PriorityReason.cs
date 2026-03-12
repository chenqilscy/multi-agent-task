namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 优先级原因枚举
    /// </summary>
    public enum PriorityReason
    {
        /// <summary>用户明确指定</summary>
        UserExplicit,
        /// <summary>安全关键</summary>
        SafetyCritical,
        /// <summary>用户交互相关</summary>
        UserInteraction,
        /// <summary>系统默认</summary>
        SystemDefault,
        /// <summary>后台任务</summary>
        BackgroundTask,
        /// <summary>依赖高优先级任务</summary>
        DependentOnHighPriority
    }
}
