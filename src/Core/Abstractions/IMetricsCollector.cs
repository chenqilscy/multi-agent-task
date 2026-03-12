using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Agent;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 指标采集上下文
    /// </summary>
    public class MetricsContext
    {
        /// <summary>操作名称</summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>附加标签</summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 监控指标收集器接口
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// 记录执行指标
        /// </summary>
        Task RecordExecutionAsync(
            string agentName,
            DateTime startTime,
            bool success,
            CancellationToken ct = default);

        /// <summary>
        /// 记录错误指标
        /// </summary>
        Task RecordErrorAsync(
            string agentName,
            Exception exception,
            CancellationToken ct = default);

        /// <summary>
        /// 记录自定义计数器
        /// </summary>
        Task IncrementCounterAsync(
            string counterName,
            Dictionary<string, string>? tags = null,
            CancellationToken ct = default);

        /// <summary>
        /// 记录自定义计时器
        /// </summary>
        Task RecordTimingAsync(
            string timerName,
            TimeSpan duration,
            Dictionary<string, string>? tags = null,
            CancellationToken ct = default);
    }
}
