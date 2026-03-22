using CKY.MultiAgentFramework.Services.Monitoring;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Monitoring;

public class NullMetricsCollectorTests
{
    [Fact]
    public void Constructor_NullLogger_ShouldNotThrow()
    {
        var collector = new NullMetricsCollector(null);
        collector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldNotThrow()
    {
        var logger = new Mock<ILogger<NullMetricsCollector>>();
        var collector = new NullMetricsCollector(logger.Object);
        collector.Should().NotBeNull();
    }

    [Fact]
    public void IncrementCounter_ShouldNotThrow()
    {
        var collector = new NullMetricsCollector(null);
        collector.IncrementCounter("test_counter");
        collector.IncrementCounter("test_counter", 5.0);
        collector.IncrementCounter("test_counter", 1, new[] { "label1", "value1" });
    }

    [Fact]
    public void RecordHistogram_ShouldNotThrow()
    {
        var collector = new NullMetricsCollector(null);
        collector.RecordHistogram("test_histogram", 0.5);
        collector.RecordHistogram("test_histogram", 1.0, new[] { "label1" });
    }

    [Fact]
    public void RecordGauge_ShouldNotThrow()
    {
        var collector = new NullMetricsCollector(null);
        collector.RecordGauge("test_gauge", 42.0);
        collector.RecordGauge("test_gauge", 100.0, new[] { "tag" });
    }

    [Fact]
    public void RecordSummary_ShouldNotThrow()
    {
        var collector = new NullMetricsCollector(null);
        collector.RecordSummary("test_summary", 0.99);
        collector.RecordSummary("test_summary", 1.5, new[] { "p50", "0.5" });
    }

    [Fact]
    public void ImplementsIPrometheusMetricsCollector()
    {
        var collector = new NullMetricsCollector(null);
        collector.Should().BeAssignableTo<IPrometheusMetricsCollector>();
    }

    [Fact]
    public void MafMetrics_ConstantsShouldBeWellDefined()
    {
        MafMetrics.HttpRequestTotal.Should().Be("maf_http_requests_total");
        MafMetrics.AgentExecutionTotal.Should().Be("maf_agent_executions_total");
        MafMetrics.TaskCreatedTotal.Should().Be("maf_task_created_total");
        MafMetrics.CacheHitsTotal.Should().Be("maf_cache_hits_total");
        MafMetrics.LlmRequestsTotal.Should().Be("maf_llm_requests_total");
        MafMetrics.DegradationLevel.Should().Be("maf_degradation_level");
        MafMetrics.MemoryUsage.Should().Be("maf_memory_usage_bytes");
        MafMetrics.SignalRConnections.Should().Be("maf_signalr_connections_total");
    }
}
