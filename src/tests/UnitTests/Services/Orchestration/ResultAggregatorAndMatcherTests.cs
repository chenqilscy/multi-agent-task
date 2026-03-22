using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Orchestration
{
    public class MafResultAggregatorTests
    {
        private readonly Mock<ILogger<MafResultAggregator>> _mockLogger = new();
        private readonly MafResultAggregator _aggregator;

        public MafResultAggregatorTests()
        {
            _aggregator = new MafResultAggregator(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var act = () => new MafResultAggregator(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task AggregateAsync_AllSuccess_ShouldReturnSuccessfulResult()
        {
            var results = new List<TaskExecutionResult>
            {
                new() { TaskId = "t1", Success = true, Message = "完成1" },
                new() { TaskId = "t2", Success = true, Message = "完成2" }
            };

            var aggregated = await _aggregator.AggregateAsync(results, "用户输入");

            aggregated.Success.Should().BeTrue();
            aggregated.Summary.Should().Contain("2");
            aggregated.IndividualResults.Should().HaveCount(2);
        }

        [Fact]
        public async Task AggregateAsync_PartialFailure_ShouldReturnFailedResult()
        {
            var results = new List<TaskExecutionResult>
            {
                new() { TaskId = "t1", Success = true },
                new() { TaskId = "t2", Success = false }
            };

            var aggregated = await _aggregator.AggregateAsync(results, "input");

            aggregated.Success.Should().BeFalse();
            aggregated.Summary.Should().Contain("1/2");
        }

        [Fact]
        public async Task AggregateAsync_EmptyResults_ShouldHandleGracefully()
        {
            var results = new List<TaskExecutionResult>();

            var aggregated = await _aggregator.AggregateAsync(results, "input");

            aggregated.Summary.Should().Contain("没有任务");
        }

        [Fact]
        public async Task AggregateAsync_WithData_ShouldAggregateData()
        {
            var results = new List<TaskExecutionResult>
            {
                new() { TaskId = "t1", Success = true, Data = "data1" },
                new() { TaskId = "t2", Success = true, Data = "data2" }
            };

            var aggregated = await _aggregator.AggregateAsync(results, "input");

            aggregated.AggregatedData.Should().HaveCount(2);
            aggregated.AggregatedData["t1"].Should().Be("data1");
        }

        [Fact]
        public async Task GenerateResponseAsync_NoResults_ShouldReturnEmptyMessage()
        {
            var aggregated = new AggregatedResult();

            var response = await _aggregator.GenerateResponseAsync(aggregated);

            response.Should().Contain("没有找到可执行的任务");
        }

        [Fact]
        public async Task GenerateResponseAsync_SuccessResults_ShouldContainMessages()
        {
            var aggregated = new AggregatedResult
            {
                IndividualResults = new List<TaskExecutionResult>
                {
                    new() { TaskId = "t1", Success = true, Message = "灯已打开" },
                    new() { TaskId = "t2", Success = true, Message = "空调已设置" }
                }
            };

            var response = await _aggregator.GenerateResponseAsync(aggregated);

            response.Should().Contain("好的");
            response.Should().Contain("灯已打开");
            response.Should().Contain("空调已设置");
        }

        [Fact]
        public async Task GenerateResponseAsync_MixedResults_ShouldReportFailures()
        {
            var aggregated = new AggregatedResult
            {
                IndividualResults = new List<TaskExecutionResult>
                {
                    new() { TaskId = "t1", Success = true, Message = "成功" },
                    new() { TaskId = "t2", Success = false, Error = "设备离线" }
                }
            };

            var response = await _aggregator.GenerateResponseAsync(aggregated);

            response.Should().Contain("设备离线");
        }

        [Fact]
        public async Task GenerateResponseAsync_AllFailed_ShouldReportFailed()
        {
            var aggregated = new AggregatedResult
            {
                Summary = "全部失败",
                IndividualResults = new List<TaskExecutionResult>
                {
                    new() { TaskId = "t1", Success = false, Error = "错误1" }
                }
            };

            var response = await _aggregator.GenerateResponseAsync(aggregated);

            response.Should().Contain("错误1");
        }
    }

    public class MafAgentMatcherTests
    {
        private readonly Mock<IAgentRegistry> _mockRegistry = new();
        private readonly Mock<ILogger<MafAgentMatcher>> _mockLogger = new();
        private readonly MafAgentMatcher _matcher;

        public MafAgentMatcherTests()
        {
            _matcher = new MafAgentMatcher(_mockRegistry.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullRegistry_ShouldThrow()
        {
            var act = () => new MafAgentMatcher(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var act = () => new MafAgentMatcher(_mockRegistry.Object, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task FindBestAgentIdAsync_FoundAgent_ShouldReturnAgentId()
        {
            var reg = new AgentRegistration { AgentId = "agent-lighting" };
            _mockRegistry.Setup(r => r.FindByCapabilityAsync("lighting", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reg);

            var result = await _matcher.FindBestAgentIdAsync("lighting");

            result.Should().Be("agent-lighting");
        }

        [Fact]
        public async Task FindBestAgentIdAsync_NoAgent_ShouldThrow()
        {
            _mockRegistry.Setup(r => r.FindByCapabilityAsync("unknown", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AgentRegistration?)null);

            var act = async () => await _matcher.FindBestAgentIdAsync("unknown");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task MatchBatchAsync_ShouldMatchAvailableTasks()
        {
            var reg = new AgentRegistration { AgentId = "agent-1" };
            _mockRegistry.Setup(r => r.FindByCapabilityAsync("cap1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reg);
            _mockRegistry.Setup(r => r.FindByCapabilityAsync("unknown", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AgentRegistration?)null);

            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "t1", RequiredCapability = "cap1" },
                new() { TaskId = "t2", RequiredCapability = "unknown" }
            };

            var result = await _matcher.MatchBatchAsync(tasks);

            result.Should().ContainKey("t1");
            result["t1"].Should().Be("agent-1");
            // t2 should not be in result since no agent found
        }

        [Fact]
        public async Task GetAvailableAgentIdsAsync_ShouldReturnAllAgentIds()
        {
            var agents = new List<AgentRegistration>
            {
                new() { AgentId = "a1" },
                new() { AgentId = "a2" },
                new() { AgentId = "a3" }
            };
            _mockRegistry.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(agents);

            var result = await _matcher.GetAvailableAgentIdsAsync();

            result.Should().HaveCount(3);
            result.Should().Contain("a1");
            result.Should().Contain("a3");
        }
    }
}
