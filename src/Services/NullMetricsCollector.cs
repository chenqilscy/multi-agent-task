using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services
{
    /// <summary>
    /// 空指标收集器（用于测试和开发，不收集真实指标）
    /// </summary>
    public class NullMetricsCollector : IMetricsCollector
    {
        public Task RecordExecutionAsync(
            string agentName,
            DateTime startTime,
            bool success,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RecordErrorAsync(
            string agentName,
            Exception exception,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task IncrementCounterAsync(
            string counterName,
            Dictionary<string, string>? tags = null,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RecordTimingAsync(
            string timerName,
            TimeSpan duration,
            Dictionary<string, string>? tags = null,
            CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
