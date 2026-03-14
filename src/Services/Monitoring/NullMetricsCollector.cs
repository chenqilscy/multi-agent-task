using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Monitoring
{
    /// <summary>
    /// 空指标收集器（用于测试和开发环境）
    /// </summary>
    public class NullMetricsCollector : IPrometheusMetricsCollector
    {
        private readonly ILogger<NullMetricsCollector>? _logger;

        public NullMetricsCollector(ILogger<NullMetricsCollector>? logger = null)
        {
            _logger = logger;
        }

        public void IncrementCounter(string name, double value = 1, string[]? labels = null)
        {
            _logger?.LogDebug("NullMetricsCollector: Incremented {Name} by {Value}", name, value);
        }

        public void RecordHistogram(string name, double value, string[]? labels = null)
        {
            _logger?.LogDebug("NullMetricsCollector: Recorded {Name}: {Value}", name, value);
        }

        public void RecordGauge(string name, double value, string[]? labels = null)
        {
            _logger?.LogDebug("NullMetricsCollector: Recorded gauge {Name}: {Value}", name, value);
        }

        public void RecordSummary(string name, double value, string[]? labels = null)
        {
            _logger?.LogDebug("NullMetricsCollector: Recorded summary {Name}: {Value}", name, value);
        }
    }
}
