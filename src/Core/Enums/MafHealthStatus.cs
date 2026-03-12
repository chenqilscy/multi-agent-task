namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 健康状态枚举
    /// </summary>
    public enum MafHealthStatus
    {
        /// <summary>正常</summary>
        Healthy = 0,
        /// <summary>降级运行</summary>
        Degraded = 1,
        /// <summary>不健康</summary>
        Unhealthy = 2,
        /// <summary>未知</summary>
        Unknown = 3
    }
}
