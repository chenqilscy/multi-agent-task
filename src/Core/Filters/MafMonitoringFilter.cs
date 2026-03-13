using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CKY.MultiAgentFramework.Core.Filters
{
    /// <summary>
    /// 监控指标 Filter（基于 RC4）
    /// </summary>
    public class MafMonitoringFilter
    {
        private readonly ILogger<MafMonitoringFilter> _logger;
        private static readonly Meter Meter = new Meter("CKY.MultiAgentFramework");
        private static readonly Counter<long> InvocationsCounter =
            Meter.CreateCounter<long>("maf_agent_invocations_total", "invocations", "Total agent invocations");
        private static readonly Histogram<double> DurationHistogram =
            Meter.CreateHistogram<double>("maf_agent_duration_seconds", "s", "Agent duration");

        public MafMonitoringFilter(ILogger<MafMonitoringFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理 Agent 运行的监控指标收集
        /// </summary>
        public async Task ProcessAsync(
            string sessionId,
            string agentName,
            Func<Task> next,
            CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 执行核心逻辑
                await next();

                // 记录指标
                var duration = stopwatch.Elapsed;
                var tags = new TagList
                {
                    { "agent", agentName },
                    { "session_id", sessionId }
                };

                InvocationsCounter.Add(1, tags);
                DurationHistogram.Record(duration.TotalSeconds, tags);

                _logger.LogInformation("[MafMonitoring] Agent: {AgentName}, Duration: {Duration}s",
                    agentName, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MafMonitoring] Error in agent: {AgentName}", agentName);
                throw;
            }
        }

        /// <summary>
        /// 记录 LLM API 延迟
        /// </summary>
        public void RecordLlmApiLatency(
            string providerName,
            string modelName,
            TimeSpan latency)
        {
            var tags = new TagList
            {
                { "provider", providerName },
                { "model", modelName }
            };

            var llmLatencyHistogram = Meter.CreateHistogram<double>("maf_llm_api_latency_seconds", "s", "LLM API latency");
            llmLatencyHistogram.Record(latency.TotalSeconds, tags);

            _logger.LogDebug("[MafMonitoring] LLM API latency: {Provider}/{Model} - {Latency}s",
                providerName, modelName, latency.TotalSeconds);
        }
    }
}
