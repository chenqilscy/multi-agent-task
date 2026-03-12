using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Agent
{
    /// <summary>
    /// Agent统计信息
    /// </summary>
    public class AgentStatistics
    {
        /// <summary>总执行次数</summary>
        public long TotalExecutions { get; set; }

        /// <summary>成功执行次数</summary>
        public long SuccessfulExecutions { get; set; }

        /// <summary>失败执行次数</summary>
        public long FailedExecutions { get; set; }

        /// <summary>平均执行时间（毫秒）</summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>最后执行时间</summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>成功率</summary>
        public double SuccessRate => TotalExecutions > 0
            ? (double)SuccessfulExecutions / TotalExecutions * 100
            : 0;
    }

    /// <summary>
    /// Agent健康检查结果
    /// </summary>
    public class AgentHealthReport
    {
        /// <summary>Agent ID</summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>健康状态</summary>
        public MafHealthStatus Status { get; set; }

        /// <summary>检查时间</summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>描述信息</summary>
        public string? Description { get; set; }

        /// <summary>详细信息</summary>
        public Dictionary<string, object> Details { get; set; } = new();
    }
}
