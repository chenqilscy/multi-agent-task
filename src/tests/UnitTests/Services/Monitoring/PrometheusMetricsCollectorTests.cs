using System.Diagnostics.Metrics;
using CKY.MultiAgentFramework.Services.Monitoring;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Services.Monitoring;

/// <summary>
/// PrometheusMetricsCollector 单元测试
/// </summary>
public class PrometheusMetricsCollectorTests : IDisposable
{
    private readonly Mock<ILogger<PrometheusMetricsCollector>> _loggerMock = new();
    private readonly IMeterFactory _meterFactory;
    private readonly PrometheusMetricsCollector _collector;
    private readonly MeterListener _listener;
    private readonly List<(string Name, double Value)> _recordedCounters = [];
    private readonly List<(string Name, double Value)> _recordedHistograms = [];

    public PrometheusMetricsCollectorTests()
    {
        _meterFactory = new TestMeterFactory();
        _collector = new PrometheusMetricsCollector(_loggerMock.Object, _meterFactory);

        // Set up a MeterListener to capture instrument recordings
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "CKY.MAF")
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            if (instrument is Counter<double>)
                _recordedCounters.Add((instrument.Name, value));
            else if (instrument is Histogram<double>)
                _recordedHistograms.Add((instrument.Name, value));
        });
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new PrometheusMetricsCollector(null!, _meterFactory);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullMeterFactory_Throws()
    {
        var act = () => new PrometheusMetricsCollector(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("meterFactory");
    }

    #endregion

    #region IncrementCounter

    [Theory]
    [InlineData(MafMetrics.HttpRequestTotal)]
    [InlineData(MafMetrics.AgentExecutionTotal)]
    [InlineData(MafMetrics.AgentExecutionErrors)]
    [InlineData(MafMetrics.TaskCreatedTotal)]
    [InlineData(MafMetrics.TaskCompletedTotal)]
    [InlineData(MafMetrics.TaskFailedTotal)]
    [InlineData(MafMetrics.CacheHitsTotal)]
    [InlineData(MafMetrics.CacheMissesTotal)]
    [InlineData(MafMetrics.LlmRequestsTotal)]
    [InlineData(MafMetrics.LlmCallsTotal)]
    [InlineData(MafMetrics.LlmRequestErrors)]
    [InlineData(MafMetrics.LlmTokensUsed)]
    [InlineData(MafMetrics.LlmPromptTokensTotal)]
    [InlineData(MafMetrics.LlmResponseTokensTotal)]
    [InlineData(MafMetrics.GcCount)]
    [InlineData(MafMetrics.SignalRConnections)]
    [InlineData(MafMetrics.SignalRMessagesSent)]
    [InlineData(MafMetrics.SignalRMessagesReceived)]
    public void IncrementCounter_KnownMetric_RecordsValue(string metricName)
    {
        _collector.IncrementCounter(metricName, 5.0);

        _recordedCounters.Should().Contain(r => r.Name == metricName && r.Value == 5.0);
    }

    [Fact]
    public void IncrementCounter_WithLabels_RecordsValue()
    {
        _collector.IncrementCounter(MafMetrics.HttpRequestTotal, 1, ["method", "GET", "status", "200"]);

        _recordedCounters.Should().Contain(r => r.Name == MafMetrics.HttpRequestTotal && r.Value == 1);
    }

    [Fact]
    public void IncrementCounter_UnknownMetric_DoesNotThrow()
    {
        var act = () => _collector.IncrementCounter("unknown_metric", 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementCounter_DefaultValue_Is1()
    {
        _collector.IncrementCounter(MafMetrics.HttpRequestTotal);

        _recordedCounters.Should().Contain(r => r.Name == MafMetrics.HttpRequestTotal && r.Value == 1);
    }

    #endregion

    #region RecordHistogram

    [Theory]
    [InlineData(MafMetrics.HttpRequestDuration)]
    [InlineData(MafMetrics.AgentExecutionDuration)]
    [InlineData(MafMetrics.LlmRequestDuration)]
    [InlineData(MafMetrics.LlmLatencySeconds)]
    [InlineData(MafMetrics.CacheDuration)]
    [InlineData(MafMetrics.GcDuration)]
    [InlineData(MafMetrics.TaskDuration)]
    public void RecordHistogram_KnownMetric_RecordsValue(string metricName)
    {
        _collector.RecordHistogram(metricName, 0.25);

        _recordedHistograms.Should().Contain(r => r.Name == metricName || r.Value == 0.25);
    }

    [Fact]
    public void RecordHistogram_WithLabels_RecordsValue()
    {
        _collector.RecordHistogram(MafMetrics.LlmLatencySeconds, 1.5, ["model", "gpt-4"]);

        _recordedHistograms.Should().Contain(r => r.Name == MafMetrics.LlmLatencySeconds && r.Value == 1.5);
    }

    [Fact]
    public void RecordHistogram_UnknownMetric_DoesNotThrow()
    {
        var act = () => _collector.RecordHistogram("unknown_histogram", 1.0);
        act.Should().NotThrow();
    }

    #endregion

    #region RecordGauge

    [Theory]
    [InlineData(MafMetrics.MemoryUsage, 1024000.0)]
    [InlineData(MafMetrics.CpuUsage, 55.5)]
    [InlineData(MafMetrics.DegradationLevel, 3.0)]
    [InlineData(MafMetrics.TaskConcurrentExecutions, 10.0)]
    [InlineData(MafMetrics.HttpRequestInProgress, 5.0)]
    public void RecordGauge_KnownMetric_DoesNotThrow(string metricName, double value)
    {
        var act = () => _collector.RecordGauge(metricName, value);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_TaskQueueDepth_DoesNotThrow()
    {
        var act = () => _collector.RecordGauge("maf_task_queue_depth", 42);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_ActiveAgents_DoesNotThrow()
    {
        var act = () => _collector.RecordGauge("maf_agents_active", 3);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_CacheHitRate_DoesNotThrow()
    {
        var act = () => _collector.RecordGauge("maf_cache_hit_rate", 0.85);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_CacheSizeBytes_DoesNotThrow()
    {
        var act = () => _collector.RecordGauge("maf_cache_size_bytes", 1024);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordGauge_UnknownMetric_DoesNotThrow()
    {
        var act = () => _collector.RecordGauge("unknown_gauge", 1.0);
        act.Should().NotThrow();
    }

    #endregion

    #region RecordSummary

    [Fact]
    public void RecordSummary_DelegatesToHistogram()
    {
        _collector.RecordSummary(MafMetrics.LlmRequestDuration, 0.5);

        // RecordSummary delegates to RecordHistogram
        _recordedHistograms.Should().Contain(r => r.Name == MafMetrics.LlmRequestDuration && r.Value == 0.5);
    }

    #endregion

    #region TestMeterFactory

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
                meter.Dispose();
        }
    }

    #endregion
}
