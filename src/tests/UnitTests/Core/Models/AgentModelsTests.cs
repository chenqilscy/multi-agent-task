using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Agent;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models
{
    public class AgentModelsTests
    {
        // ========== AgentStatistics ==========

        [Fact]
        public void AgentStatistics_SuccessRate_WhenNoExecutions_ReturnsZero()
        {
            var stats = new AgentStatistics();

            stats.SuccessRate.Should().Be(0);
        }

        [Fact]
        public void AgentStatistics_SuccessRate_WhenAllSuccessful_Returns100()
        {
            var stats = new AgentStatistics
            {
                TotalExecutions = 10,
                SuccessfulExecutions = 10
            };

            stats.SuccessRate.Should().Be(100);
        }

        [Fact]
        public void AgentStatistics_SuccessRate_CalculatesCorrectly()
        {
            var stats = new AgentStatistics
            {
                TotalExecutions = 200,
                SuccessfulExecutions = 150
            };

            stats.SuccessRate.Should().Be(75);
        }

        [Fact]
        public void AgentStatistics_DefaultProperties()
        {
            var stats = new AgentStatistics();

            stats.TotalExecutions.Should().Be(0);
            stats.SuccessfulExecutions.Should().Be(0);
            stats.FailedExecutions.Should().Be(0);
            stats.AverageExecutionTimeMs.Should().Be(0);
            stats.LastExecutionTime.Should().BeNull();
        }

        // ========== AgentHealthReport ==========

        [Fact]
        public void AgentHealthReport_DefaultProperties()
        {
            var report = new AgentHealthReport();

            report.AgentId.Should().BeEmpty();
            report.Status.Should().Be(MafHealthStatus.Healthy);
            report.Description.Should().BeNull();
            report.Details.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void AgentHealthReport_CanSetAllProperties()
        {
            var report = new AgentHealthReport
            {
                AgentId = "agent-1",
                Status = MafHealthStatus.Degraded,
                Description = "High latency",
                Details = new Dictionary<string, object>
                {
                    ["latency_ms"] = 500,
                    ["error_rate"] = 0.05
                }
            };

            report.AgentId.Should().Be("agent-1");
            report.Status.Should().Be(MafHealthStatus.Degraded);
            report.Description.Should().Be("High latency");
            report.Details.Should().HaveCount(2);
        }

        [Theory]
        [InlineData(MafHealthStatus.Healthy)]
        [InlineData(MafHealthStatus.Degraded)]
        [InlineData(MafHealthStatus.Unhealthy)]
        [InlineData(MafHealthStatus.Unknown)]
        public void MafHealthStatus_ShouldBeDefined(MafHealthStatus status)
        {
            Enum.IsDefined(typeof(MafHealthStatus), status).Should().BeTrue();
        }
    }
}
