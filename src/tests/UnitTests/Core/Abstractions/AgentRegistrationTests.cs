using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Abstractions;

public class AgentRegistrationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var reg = new AgentRegistration();
        reg.AgentId.Should().BeEmpty();
        reg.Name.Should().BeEmpty();
        reg.Description.Should().BeEmpty();
        reg.Version.Should().Be("1.0.0");
        reg.Capabilities.Should().BeEmpty();
        reg.Status.Should().Be(MafAgentStatus.Idle);
        reg.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        reg.LastHeartbeat.Should().BeNull();
        reg.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_ShouldWork()
    {
        var now = DateTime.UtcNow;
        var reg = new AgentRegistration
        {
            AgentId = "agent-1",
            Name = "TestAgent",
            Description = "A test agent",
            Version = "2.0.0",
            Capabilities = new List<string> { "chat", "translate" },
            Status = MafAgentStatus.Busy,
            RegisteredAt = now,
            LastHeartbeat = now,
            Metadata = new Dictionary<string, object> { ["region"] = "us-east" }
        };

        reg.AgentId.Should().Be("agent-1");
        reg.Name.Should().Be("TestAgent");
        reg.Description.Should().Be("A test agent");
        reg.Version.Should().Be("2.0.0");
        reg.Capabilities.Should().HaveCount(2);
        reg.Status.Should().Be(MafAgentStatus.Busy);
        reg.LastHeartbeat.Should().Be(now);
        reg.Metadata.Should().ContainKey("region");
    }
}
