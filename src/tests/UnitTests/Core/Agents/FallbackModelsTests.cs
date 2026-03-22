using CKY.MultiAgentFramework.Core.Agents;
using FluentAssertions;

namespace CKY.MAF.Tests.Core.Agents;

/// <summary>
/// FallbackAttempt 和 FallbackStatistics 模型测试
/// </summary>
public class FallbackModelsTests
{
    // === FallbackAttempt ===

    [Fact]
    public void FallbackAttempt_Defaults()
    {
        var attempt = new FallbackAttempt();
        attempt.Attempts.Should().BeEmpty();
        attempt.SuccessAgentId.Should().BeNull();
        attempt.ErrorMessage.Should().BeNull();
        attempt.Prompt.Should().BeNull();
    }

    [Fact]
    public void FallbackAttempt_Duration_Computed()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddSeconds(5);

        var attempt = new FallbackAttempt
        {
            StartTime = start,
            EndTime = end,
            Attempts = new List<string> { "agent-1", "agent-2" },
            SuccessAgentId = "agent-2",
            Prompt = "测试提示词"
        };

        attempt.Duration.Should().Be(TimeSpan.FromSeconds(5));
        attempt.Attempts.Should().HaveCount(2);
        attempt.SuccessAgentId.Should().Be("agent-2");
    }

    [Fact]
    public void FallbackAttempt_ErrorMessage_WhenAllFailed()
    {
        var attempt = new FallbackAttempt
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(10),
            Attempts = new List<string> { "a1", "a2", "a3" },
            ErrorMessage = "All 3 agents failed. Last error: timeout"
        };

        attempt.SuccessAgentId.Should().BeNull();
        attempt.ErrorMessage.Should().Contain("All 3 agents failed");
    }

    // === FallbackStatistics ===

    [Fact]
    public void FallbackStatistics_Defaults()
    {
        var stats = new FallbackStatistics();
        stats.TotalRequests.Should().Be(0);
        stats.SuccessfulRequests.Should().Be(0);
        stats.FallbackRate.Should().Be(0);
        stats.AgentUsageCounts.Should().BeEmpty();
        stats.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void FallbackStatistics_SuccessRate_Calculated()
    {
        var stats = new FallbackStatistics
        {
            TotalRequests = 10,
            SuccessfulRequests = 8,
            FallbackRate = 0.3
        };
        stats.AgentUsageCounts["agent-1"] = 7;
        stats.AgentUsageCounts["agent-2"] = 1;

        stats.SuccessRate.Should().Be(0.8);
        stats.AgentUsageCounts.Should().HaveCount(2);
    }

    [Fact]
    public void FallbackStatistics_ZeroRequests_SuccessRateIsZero()
    {
        var stats = new FallbackStatistics { TotalRequests = 0, SuccessfulRequests = 0 };
        stats.SuccessRate.Should().Be(0);
    }
}
