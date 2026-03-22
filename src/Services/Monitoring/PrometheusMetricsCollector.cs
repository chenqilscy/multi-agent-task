using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Monitoring
{
    /// <summary>
    /// Prometheus指标收集器实现
    /// </summary>
    public class PrometheusMetricsCollector : IPrometheusMetricsCollector
    {
        private readonly ILogger<PrometheusMetricsCollector> _logger;
        private readonly Meter _meter;

        // Counter instruments
        private readonly Counter<double> _httpRequestsTotal;
        private readonly Counter<double> _agentTasksTotal;
        private readonly Counter<double> _agentErrorsTotal;
        private readonly Counter<double> _tasksScheduledTotal;
        private readonly Counter<double> _tasksCompletedTotal;
        private readonly Counter<double> _tasksFailedTotal;
        private readonly Counter<double> _cacheHitsTotal;
        private readonly Counter<double> _cacheMissesTotal;
        private readonly Counter<double> _llmRequestsTotal;
        private readonly Counter<double> _llmCallsTotal;
        private readonly Counter<double> _llmErrorsTotal;
        private readonly Counter<double> _llmTokensUsed;
        private readonly Counter<double> _llmPromptTokensTotal;
        private readonly Counter<double> _llmResponseTokensTotal;
        private readonly Counter<double> _gcCount;
        private readonly Counter<double> _signalRConnections;
        private readonly Counter<double> _signalRMessagesSent;
        private readonly Counter<double> _signalRMessagesReceived;

        // Histogram instruments
        private readonly Histogram<double> _httpRequestDuration;
        private readonly Histogram<double> _agentTaskDuration;
        private readonly Histogram<double> _llmRequestDuration;
        private readonly Histogram<double> _llmLatencySeconds;
        private readonly Histogram<double> _cacheDuration;
        private readonly Histogram<double> _gcDuration;

        // Gauge instruments
        private readonly ObservableGauge<double> _httpRequestsInProgress;
        private readonly ObservableGauge<double> _taskQueueDepth;
        private readonly ObservableGauge<double> _activeAgents;
        private readonly ObservableGauge<double> _cacheHitRate;
        private readonly ObservableGauge<double> _cacheSizeBytes;
        private readonly ObservableGauge<double> _memoryUsage;
        private readonly ObservableGauge<double> _cpuUsage;
        private readonly ObservableGauge<double> _degradationLevel;
        private readonly ObservableGauge<double> _taskConcurrentExecutions;

        private int _httpInProgress;
        private int _taskQueueDepthValue;
        private int _activeAgentsValue;
        private double _cacheHitRateValue;
        private long _cacheSizeBytesValue;
        private double _memoryUsageValue;
        private double _cpuUsageValue;
        private int _degradationLevelValue;
        private int _taskConcurrentExecutionsValue;

        public PrometheusMetricsCollector(
            ILogger<PrometheusMetricsCollector> logger,
            IMeterFactory meterFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (meterFactory == null) throw new ArgumentNullException(nameof(meterFactory));

            _meter = meterFactory.Create("CKY.MAF", "1.0.0");

            // Initialize Counters
            _httpRequestsTotal = _meter.CreateCounter<double>(
                MafMetrics.HttpRequestTotal,
                "{request}",
                "Total HTTP requests"
            );

            _agentTasksTotal = _meter.CreateCounter<double>(
                MafMetrics.AgentExecutionTotal,
                "{task}",
                "Total agent tasks executed"
            );

            _agentErrorsTotal = _meter.CreateCounter<double>(
                MafMetrics.AgentExecutionErrors,
                "{error}",
                "Total agent errors"
            );

            _tasksScheduledTotal = _meter.CreateCounter<double>(
                MafMetrics.TaskCreatedTotal,
                "{task}",
                "Total tasks scheduled"
            );

            _tasksCompletedTotal = _meter.CreateCounter<double>(
                MafMetrics.TaskCompletedTotal,
                "{task}",
                "Total tasks completed"
            );

            _tasksFailedTotal = _meter.CreateCounter<double>(
                MafMetrics.TaskFailedTotal,
                "{task}",
                "Total tasks failed"
            );

            _cacheHitsTotal = _meter.CreateCounter<double>(
                MafMetrics.CacheHitsTotal,
                "{hit}",
                "Total cache hits"
            );

            _cacheMissesTotal = _meter.CreateCounter<double>(
                MafMetrics.CacheMissesTotal,
                "{miss}",
                "Total cache misses"
            );

            _llmRequestsTotal = _meter.CreateCounter<double>(
                MafMetrics.LlmRequestsTotal,
                "{request}",
                "Total LLM requests"
            );

            _llmCallsTotal = _meter.CreateCounter<double>(
                MafMetrics.LlmCallsTotal,
                "{call}",
                "Total LLM calls (with provider/status labels)"
            );

            _llmErrorsTotal = _meter.CreateCounter<double>(
                MafMetrics.LlmRequestErrors,
                "{error}",
                "Total LLM errors"
            );

            _llmTokensUsed = _meter.CreateCounter<double>(
                MafMetrics.LlmTokensUsed,
                "{token}",
                "Total LLM tokens used"
            );

            _llmPromptTokensTotal = _meter.CreateCounter<double>(
                MafMetrics.LlmPromptTokensTotal,
                "{token}",
                "Total LLM prompt tokens used"
            );

            _llmResponseTokensTotal = _meter.CreateCounter<double>(
                MafMetrics.LlmResponseTokensTotal,
                "{token}",
                "Total LLM response tokens used"
            );

            _gcCount = _meter.CreateCounter<double>(
                MafMetrics.GcCount,
                "{collection}",
                "Total GC collections"
            );

            _signalRConnections = _meter.CreateCounter<double>(
                MafMetrics.SignalRConnections,
                "{connection}",
                "Total SignalR connections"
            );

            _signalRMessagesSent = _meter.CreateCounter<double>(
                MafMetrics.SignalRMessagesSent,
                "{message}",
                "Total SignalR messages sent"
            );

            _signalRMessagesReceived = _meter.CreateCounter<double>(
                MafMetrics.SignalRMessagesReceived,
                "{message}",
                "Total SignalR messages received"
            );

            // Initialize Histograms
            _httpRequestDuration = _meter.CreateHistogram<double>(
                MafMetrics.HttpRequestDuration,
                "s",
                "HTTP request duration in seconds"
            );

            _agentTaskDuration = _meter.CreateHistogram<double>(
                MafMetrics.AgentExecutionDuration,
                "s",
                "Agent task execution duration in seconds"
            );

            _llmRequestDuration = _meter.CreateHistogram<double>(
                MafMetrics.LlmRequestDuration,
                "s",
                "LLM request duration in seconds"
            );

            _llmLatencySeconds = _meter.CreateHistogram<double>(
                MafMetrics.LlmLatencySeconds,
                "s",
                "LLM latency in seconds (with model labels, for dashboard)"
            );

            _cacheDuration = _meter.CreateHistogram<double>(
                MafMetrics.CacheDuration,
                "s",
                "Cache operation duration in seconds"
            );

            _gcDuration = _meter.CreateHistogram<double>(
                MafMetrics.GcDuration,
                "s",
                "GC duration in seconds"
            );

            // Initialize Gauges
            _httpRequestsInProgress = _meter.CreateObservableGauge<double>(
                MafMetrics.HttpRequestInProgress,
                () => new Measurement<double>(_httpInProgress),
                "{request}",
                "HTTP requests currently in progress"
            );

            _taskQueueDepth = _meter.CreateObservableGauge<double>(
                "maf_task_queue_depth",
                () => new Measurement<double>(_taskQueueDepthValue),
                "{task}",
                "Current task queue depth"
            );

            _activeAgents = _meter.CreateObservableGauge<double>(
                "maf_agents_active",
                () => new Measurement<double>(_activeAgentsValue),
                "{agent}",
                "Currently active agents"
            );

            _cacheHitRate = _meter.CreateObservableGauge<double>(
                "maf_cache_hit_rate",
                () => new Measurement<double>(_cacheHitRateValue),
                "{rate}",
                "Cache hit rate (0-1)"
            );

            _cacheSizeBytes = _meter.CreateObservableGauge<double>(
                "maf_cache_size_bytes",
                () => new Measurement<double>(_cacheSizeBytesValue),
                "{byte}",
                "Current cache size in bytes"
            );

            _memoryUsage = _meter.CreateObservableGauge<double>(
                MafMetrics.MemoryUsage,
                () => new Measurement<double>(_memoryUsageValue),
                "{byte}",
                "Memory usage in bytes"
            );

            _cpuUsage = _meter.CreateObservableGauge<double>(
                MafMetrics.CpuUsage,
                () => new Measurement<double>(_cpuUsageValue),
                "{percent}",
                "CPU usage percentage"
            );

            _degradationLevel = _meter.CreateObservableGauge<double>(
                MafMetrics.DegradationLevel,
                () => new Measurement<double>(_degradationLevelValue),
                "{level}",
                "Current degradation level (0=normal, 1-5=degraded)"
            );

            _taskConcurrentExecutions = _meter.CreateObservableGauge<double>(
                MafMetrics.TaskConcurrentExecutions,
                () => new Measurement<double>(_taskConcurrentExecutionsValue),
                "{task}",
                "Current number of concurrently executing tasks"
            );
        }

        /// <inheritdoc />
        public void IncrementCounter(string name, double value = 1, string[]? labels = null)
        {
            try
            {
                var counter = name switch
                {
                    MafMetrics.HttpRequestTotal => _httpRequestsTotal,
                    MafMetrics.AgentExecutionTotal => _agentTasksTotal,
                    MafMetrics.AgentExecutionErrors => _agentErrorsTotal,
                    MafMetrics.TaskCreatedTotal => _tasksScheduledTotal,
                    MafMetrics.TaskCompletedTotal => _tasksCompletedTotal,
                    MafMetrics.TaskFailedTotal => _tasksFailedTotal,
                    MafMetrics.CacheHitsTotal => _cacheHitsTotal,
                    MafMetrics.CacheMissesTotal => _cacheMissesTotal,
                    MafMetrics.LlmRequestsTotal => _llmRequestsTotal,
                    MafMetrics.LlmCallsTotal => _llmCallsTotal,
                    MafMetrics.LlmRequestErrors => _llmErrorsTotal,
                    MafMetrics.LlmTokensUsed => _llmTokensUsed,
                    MafMetrics.LlmPromptTokensTotal => _llmPromptTokensTotal,
                    MafMetrics.LlmResponseTokensTotal => _llmResponseTokensTotal,
                    MafMetrics.GcCount => _gcCount,
                    MafMetrics.SignalRConnections => _signalRConnections,
                    MafMetrics.SignalRMessagesSent => _signalRMessagesSent,
                    MafMetrics.SignalRMessagesReceived => _signalRMessagesReceived,
                    _ => null
                };

                if (counter != null)
                {
                    if (labels != null && labels.Length > 0)
                    {
                        counter.Add(value, GetKeyValues(labels));
                    }
                    else
                    {
                        counter.Add(value);
                    }

                    _logger.LogDebug("Incremented counter {Name} by {Value}", name, value);
                }
                else
                {
                    _logger.LogWarning("Unknown counter metric: {Name}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to increment counter {Name}", name);
            }
        }

        /// <inheritdoc />
        public void RecordHistogram(string name, double value, string[]? labels = null)
        {
            try
            {
                var histogram = name switch
                {
                    MafMetrics.HttpRequestDuration => _httpRequestDuration,
                    MafMetrics.AgentExecutionDuration => _agentTaskDuration,
                    MafMetrics.LlmRequestDuration => _llmRequestDuration,
                    MafMetrics.LlmLatencySeconds => _llmLatencySeconds,
                    MafMetrics.CacheDuration => _cacheDuration,
                    MafMetrics.GcDuration => _gcDuration,
                    MafMetrics.TaskDuration => _agentTaskDuration,
                    _ => null
                };

                if (histogram != null)
                {
                    if (labels != null && labels.Length > 0)
                    {
                        histogram.Record(value, GetKeyValues(labels));
                    }
                    else
                    {
                        histogram.Record(value);
                    }

                    _logger.LogDebug("Recorded histogram {Name}: {Value}", name, value);
                }
                else
                {
                    _logger.LogWarning("Unknown histogram metric: {Name}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record histogram {Name}", name);
            }
        }

        /// <inheritdoc />
        public void RecordGauge(string name, double value, string[]? labels = null)
        {
            try
            {
                switch (name)
                {
                    case MafMetrics.MemoryUsage:
                        _memoryUsageValue = value;
                        break;
                    case MafMetrics.CpuUsage:
                        _cpuUsageValue = value;
                        break;
                    case "maf_task_queue_depth":
                        _taskQueueDepthValue = (int)value;
                        break;
                    case "maf_agents_active":
                        _activeAgentsValue = (int)value;
                        break;
                    case MafMetrics.HttpRequestInProgress:
                        _httpInProgress = (int)value;
                        break;
                    case "maf_cache_hit_rate":
                        _cacheHitRateValue = value;
                        break;
                    case "maf_cache_size_bytes":
                        _cacheSizeBytesValue = (long)value;
                        break;
                    case MafMetrics.DegradationLevel:
                        _degradationLevelValue = (int)value;
                        break;
                    case MafMetrics.TaskConcurrentExecutions:
                        _taskConcurrentExecutionsValue = (int)value;
                        break;
                    default:
                        _logger.LogWarning("Unknown gauge metric: {Name}", name);
                        break;
                }

                _logger.LogDebug("Recorded gauge {Name}: {Value}", name, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record gauge {Name}", name);
            }
        }

        /// <inheritdoc />
        public void RecordSummary(string name, double value, string[]? labels = null)
        {
            // Summary is recorded similarly to histogram in our implementation
            RecordHistogram(name, value, labels);
        }

        private KeyValuePair<string, object?>[] GetKeyValues(string[] labels)
        {
            var result = new List<KeyValuePair<string, object?>>();

            for (int i = 0; i < labels.Length; i += 2)
            {
                if (i + 1 < labels.Length)
                {
                    result.Add(new KeyValuePair<string, object?>(labels[i], labels[i + 1]));
                }
            }

            return result.ToArray();
        }
    }
}
