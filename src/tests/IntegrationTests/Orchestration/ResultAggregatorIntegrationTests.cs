using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Orchestration;

/// <summary>
/// MafResultAggregator 集成测试
/// 验证多任务结果聚合和响应生成
/// </summary>
public class ResultAggregatorIntegrationTests
{
    private readonly MafResultAggregator _aggregator;

    public ResultAggregatorIntegrationTests()
    {
        _aggregator = new MafResultAggregator(
            NullLogger<MafResultAggregator>.Instance);
    }

    [Fact]
    public async Task AggregateAsync_AllSuccess_ShouldBeSuccessful()
    {
        var results = new List<TaskExecutionResult>
        {
            new() { TaskId = "t1", Success = true, Message = "任务1完成" },
            new() { TaskId = "t2", Success = true, Message = "任务2完成" },
            new() { TaskId = "t3", Success = true, Message = "任务3完成" }
        };

        var aggregated = await _aggregator.AggregateAsync(results, "用户测试输入");

        aggregated.Success.Should().BeTrue();
        aggregated.IndividualResults.Should().HaveCount(3);
        aggregated.Summary.Should().Contain("3");
        aggregated.Summary.Should().Contain("成功");
    }

    [Fact]
    public async Task AggregateAsync_PartialFailure_ShouldNotBeSuccess()
    {
        var results = new List<TaskExecutionResult>
        {
            new() { TaskId = "t1", Success = true, Message = "成功" },
            new() { TaskId = "t2", Success = false, Error = "失败了" }
        };

        var aggregated = await _aggregator.AggregateAsync(results, "用户输入");

        aggregated.Success.Should().BeFalse("部分失败应标记为不成功");
        aggregated.Summary.Should().Contain("1/2");
    }

    [Fact]
    public async Task AggregateAsync_EmptyResults_ShouldHandleGracefully()
    {
        var aggregated = await _aggregator.AggregateAsync(
            new List<TaskExecutionResult>(), "空输入");

        aggregated.Summary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AggregateAsync_WithData_ShouldCollectAggregatedData()
    {
        var results = new List<TaskExecutionResult>
        {
            new() { TaskId = "t1", Success = true, Data = new { Code = "生成的代码" } },
            new() { TaskId = "t2", Success = true, Data = new { Report = "分析报告" } }
        };

        var aggregated = await _aggregator.AggregateAsync(results, "输入");

        aggregated.AggregatedData.Should().ContainKey("t1");
        aggregated.AggregatedData.Should().ContainKey("t2");
    }

    [Fact]
    public async Task GenerateResponseAsync_WithResults_ShouldReturnText()
    {
        var results = new List<TaskExecutionResult>
        {
            new() { TaskId = "t1", Success = true, Message = "完成任务" }
        };
        var aggregated = await _aggregator.AggregateAsync(results, "测试");

        var response = await _aggregator.GenerateResponseAsync(aggregated);

        response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateResponseAsync_NoResults_ShouldReturnFallbackMessage()
    {
        var aggregated = new AggregatedResult
        {
            Success = false,
            IndividualResults = new List<TaskExecutionResult>()
        };

        var response = await _aggregator.GenerateResponseAsync(aggregated);

        response.Should().NotBeNullOrEmpty();
    }
}
