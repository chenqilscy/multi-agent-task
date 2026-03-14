using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Agent;
using CKY.MultiAgentFramework.Services.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Storage
{
    public class MafAgentRegistryTests
    {
        private readonly Mock<ICacheStore> _mockCache;
        private readonly MafAgentRegistry _sut;

        public MafAgentRegistryTests()
        {
            _mockCache = new Mock<ICacheStore>();
            _sut = new MafAgentRegistry(_mockCache.Object, NullLogger<MafAgentRegistry>.Instance);
        }

        [Fact]
        public async Task RegisterAsync_ShouldStoreRegistrationAndAddToAllIds()
        {
            // Arrange
            var registration = new AgentRegistration
            {
                AgentId = "agent:001",
                Name = "TestAgent",
                Description = "Test Description",
                Capabilities = new List<string> { "test:capability" },
                Status = MafAgentStatus.Idle
            };

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

            // Act
            await _sut.RegisterAsync(registration);

            // Assert
            _mockCache.Verify(x => x.SetAsync(
                "maf:agent:agent:001",
                It.Is<AgentRegistration>(r =>
                    r.AgentId == "agent:001" &&
                    r.Name == "TestAgent" &&
                    r.Status == MafAgentStatus.Idle &&
                    r.RegisteredAt != default),
                TimeSpan.FromDays(1),
                default), Times.Once);

            _mockCache.Verify(x => x.SetAsync(
                "maf:agent:_all_ids",
                It.Is<List<string>>(ids => ids.Contains("agent:001")),
                TimeSpan.FromDays(7),
                default), Times.Once);
        }

        [Fact]
        public async Task UnregisterAsync_ShouldRemoveRegistrationAndFromAllIds()
        {
            // Arrange
            var agentId = "agent:001";
            var existingIds = new List<string> { "agent:001", "agent:002" };

            _mockCache.Setup(x => x.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

            _mockCache.Setup(x => x.GetAsync<List<string>>(
                "maf:agent:_all_ids",
                default))
            .ReturnsAsync(existingIds);

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

            // Act
            await _sut.UnregisterAsync(agentId);

            // Assert
            _mockCache.Verify(x => x.DeleteAsync(
                "maf:agent:agent:001",
                default), Times.Once);

            _mockCache.Verify(x => x.SetAsync(
                "maf:agent:_all_ids",
                It.Is<List<string>>(ids => !ids.Contains("agent:001") && ids.Count == 1),
                TimeSpan.FromDays(7),
                default), Times.Once);
        }

        [Fact]
        public async Task FindByIdAsync_ShouldReturnAgentWhenExists()
        {
            // Arrange
            var expected = new AgentRegistration
            {
                AgentId = "agent:001",
                Name = "TestAgent",
                Capabilities = new List<string> { "test:capability" }
            };

            _mockCache.Setup(x => x.GetAsync<AgentRegistration>(
                "maf:agent:agent:001",
                default))
            .ReturnsAsync(expected);

            // Act
            var result = await _sut.FindByIdAsync("agent:001");

            // Assert
            result.Should().NotBeNull();
            result!.AgentId.Should().Be("agent:001");
            result.Name.Should().Be("TestAgent");
        }

        [Fact]
        public async Task FindByIdAsync_ShouldReturnNullWhenNotExists()
        {
            // Arrange
            _mockCache.Setup(x => x.GetAsync<AgentRegistration>(
                It.IsAny<string>(),
                default))
            .ReturnsAsync((AgentRegistration?)null);

            // Act
            var result = await _sut.FindByIdAsync("nonexistent");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task FindByCapabilityAsync_ShouldReturnFirstIdleAgentWithCapability()
        {
            // Arrange
            var allAgents = new List<AgentRegistration>
            {
                new()
                {
                    AgentId = "agent:001",
                    Name = "Agent1",
                    Status = MafAgentStatus.Idle,
                    Capabilities = new List<string> { "lighting", "climate" }
                },
                new()
                {
                    AgentId = "agent:002",
                    Name = "Agent2",
                    Status = MafAgentStatus.Busy,
                    Capabilities = new List<string> { "lighting" }
                },
                new()
                {
                    AgentId = "agent:003",
                    Name = "Agent3",
                    Status = MafAgentStatus.Idle,
                    Capabilities = new List<string> { "lighting" }
                }
            };

            _mockCache.Setup(x => x.GetAsync<List<string>>(
                "maf:agent:_all_ids",
                default))
            .ReturnsAsync(allAgents.Select(a => a.AgentId).ToList());

            foreach (var agent in allAgents)
            {
                _mockCache.Setup(x => x.GetAsync<AgentRegistration>(
                    $"maf:agent:{agent.AgentId}",
                    default))
                .ReturnsAsync(agent);
            }

            // Act
            var result = await _sut.FindByCapabilityAsync("lighting");

            // Assert
            result.Should().NotBeNull();
            result!.AgentId.Should().Be("agent:001"); // 第一个空闲的lighting agent
            result.Status.Should().Be(MafAgentStatus.Idle);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllRegisteredAgents()
        {
            // Arrange
            var agentIds = new List<string> { "agent:001", "agent:002", "agent:003" };
            var agents = new List<AgentRegistration>
            {
                new() { AgentId = "agent:001", Name = "Agent1" },
                new() { AgentId = "agent:002", Name = "Agent2" },
                new() { AgentId = "agent:003", Name = "Agent3" }
            };

            _mockCache.Setup(x => x.GetAsync<List<string>>(
                "maf:agent:_all_ids",
                default))
            .ReturnsAsync(agentIds);

            foreach (var agent in agents)
            {
                _mockCache.Setup(x => x.GetAsync<AgentRegistration>(
                    $"maf:agent:{agent.AgentId}",
                    default))
                .ReturnsAsync(agent);
            }

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Select(a => a.AgentId).Should().BeEquivalentTo(agentIds);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyListWhenNoAgentsRegistered()
        {
            // Arrange
            _mockCache.Setup(x => x.GetAsync<List<string>>(
                "maf:agent:_all_ids",
                default))
            .ReturnsAsync((List<string>?)null);

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldUpdateStatusAndHeartbeat()
        {
            // Arrange
            var existing = new AgentRegistration
            {
                AgentId = "agent:001",
                Name = "TestAgent",
                Status = MafAgentStatus.Idle,
                LastHeartbeat = DateTime.UtcNow.AddMinutes(-5)
            };

            _mockCache.Setup(x => x.GetAsync<AgentRegistration>(
                "maf:agent:agent:001",
                default))
            .ReturnsAsync(existing);

            var statusUpdated = false;
            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object, TimeSpan?, CancellationToken>((key, value, _, _) =>
            {
                if (key == "maf:agent:agent:001")
                {
                    var agent = (AgentRegistration)value;
                    if (agent.Status == MafAgentStatus.Busy)
                    {
                        statusUpdated = true;
                    }
                }
            })
            .Returns(Task.CompletedTask);

            // Act
            await _sut.UpdateStatusAsync("agent:001", MafAgentStatus.Busy);

            // Assert
            statusUpdated.Should().BeTrue();
            _mockCache.Verify(x => x.SetAsync(
                "maf:agent:agent:001",
                It.IsAny<AgentRegistration>(),
                It.IsAny<TimeSpan?>(),
                default), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_MultipleAgents_ShouldMaintainAllIdsList()
        {
            // Arrange
            var agents = new[]
            {
                new AgentRegistration { AgentId = "agent:001", Name = "Agent1" },
                new AgentRegistration { AgentId = "agent:002", Name = "Agent2" },
                new AgentRegistration { AgentId = "agent:003", Name = "Agent3" }
            };

            var allIds = new List<string>();
            var callCount = 0;

            _mockCache.Setup(x => x.GetAsync<List<string>>(
                "maf:agent:_all_ids",
                default))
            .ReturnsAsync(() => callCount == 0 ? null : new List<string>(allIds));

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object, TimeSpan?, CancellationToken>((key, value, _, _) =>
            {
                if (key == "maf:agent:_all_ids")
                {
                    allIds = ((List<string>)value).ToList();
                    callCount++;
                }
            })
            .Returns(Task.CompletedTask);

            // Act
            foreach (var agent in agents)
            {
                await _sut.RegisterAsync(agent);
            }

            // Assert
            allIds.Should().HaveCount(3);
            allIds.Should().BeEquivalentTo(new[] { "agent:001", "agent:002", "agent:003" });
        }
    }
}
