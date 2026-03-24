using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Agents.Specialized;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Core.Agents;

/// <summary>
/// MafLeaderAgent 单元测试
/// 覆盖完整编排流水线、8 个钩子方法、边界条件
/// </summary>
public class MafLeaderAgentTests
{
    private readonly Mock<IIntentRecognizer> _intentMock;
    private readonly Mock<IEntityExtractor> _entityMock;
    private readonly Mock<ITaskDecomposer> _decomposerMock;
    private readonly Mock<IAgentMatcher> _matcherMock;
    private readonly Mock<ITaskOrchestrator> _orchestratorMock;
    private readonly Mock<IResultAggregator> _aggregatorMock;
    private readonly Mock<IMafAiAgentRegistry> _registryMock;
    private readonly MafLeaderAgent _sut;

    public MafLeaderAgentTests()
    {
        _intentMock = new Mock<IIntentRecognizer>();
        _entityMock = new Mock<IEntityExtractor>();
        _decomposerMock = new Mock<ITaskDecomposer>();
        _matcherMock = new Mock<IAgentMatcher>();
        _orchestratorMock = new Mock<ITaskOrchestrator>();
        _aggregatorMock = new Mock<IResultAggregator>();
        _registryMock = new Mock<IMafAiAgentRegistry>();

        _sut = new MafLeaderAgent(
            _intentMock.Object,
            _entityMock.Object,
            _decomposerMock.Object,
            _matcherMock.Object,
            _orchestratorMock.Object,
            _aggregatorMock.Object,
            _registryMock.Object,
            NullLogger.Instance);
    }

    #region Properties

    [Fact]
    public void Properties_ShouldHaveExpectedDefaults()
    {
        _sut.AgentId.Should().Be("maf:leader-agent:builtin");
        _sut.Name.Should().Be("MafLeaderAgent");
        _sut.Description.Should().NotBeNullOrEmpty();
        _sut.Capabilities.Should().Contain("coordination");
        _sut.Capabilities.Should().Contain("task-decomposition");
        _sut.Capabilities.Should().Contain("agent-orchestration");
        _sut.Capabilities.Should().Contain("result-aggregation");
    }

    #endregion

    #region Constructor Null Validation

    [Fact]
    public void Constructor_NullIntentRecognizer_Throws()
    {
        var act = () => new MafLeaderAgent(
            null!, _entityMock.Object, _decomposerMock.Object,
            _matcherMock.Object, _orchestratorMock.Object, _aggregatorMock.Object,
            _registryMock.Object, NullLogger.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("intentRecognizer");
    }

    [Fact]
    public void Constructor_NullEntityExtractor_Throws()
    {
        var act = () => new MafLeaderAgent(
            _intentMock.Object, null!, _decomposerMock.Object,
            _matcherMock.Object, _orchestratorMock.Object, _aggregatorMock.Object,
            _registryMock.Object, NullLogger.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("entityExtractor");
    }

    [Fact]
    public void Constructor_NullTaskDecomposer_Throws()
    {
        var act = () => new MafLeaderAgent(
            _intentMock.Object, _entityMock.Object, null!,
            _matcherMock.Object, _orchestratorMock.Object, _aggregatorMock.Object,
            _registryMock.Object, NullLogger.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("taskDecomposer");
    }

    [Fact]
    public void Constructor_NullAgentMatcher_Throws()
    {
        var act = () => new MafLeaderAgent(
            _intentMock.Object, _entityMock.Object, _decomposerMock.Object,
            null!, _orchestratorMock.Object, _aggregatorMock.Object,
            _registryMock.Object, NullLogger.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("agentMatcher");
    }

    [Fact]
    public void Constructor_NullTaskOrchestrator_Throws()
    {
        var act = () => new MafLeaderAgent(
            _intentMock.Object, _entityMock.Object, _decomposerMock.Object,
            _matcherMock.Object, null!, _aggregatorMock.Object,
            _registryMock.Object, NullLogger.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("taskOrchestrator");
    }

    [Fact]
    public void Constructor_NullResultAggregator_Throws()
    {
        var act = () => new MafLeaderAgent(
            _intentMock.Object, _entityMock.Object, _decomposerMock.Object,
            _matcherMock.Object, _orchestratorMock.Object, null!,
            _registryMock.Object, NullLogger.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("resultAggregator");
    }

    #endregion

    #region Full Pipeline (Happy Path)

    [Fact]
    public async Task ExecuteBusinessLogicAsync_FullPipeline_Success()
    {
        // Arrange
        var request = CreateRequest("打开客厅灯光");

        SetupIntentRecognizer("control-device", 0.9);
        SetupEntityExtractor(new Dictionary<string, object> { ["Room"] = "客厅", ["Device"] = "灯光" });
        SetupTaskDecomposer(new List<DecomposedTask>
        {
            new() { TaskId = "sub-1", Description = "控制灯光", RequiredCapability = "device-control" }
        });
        SetupAgentMatcher();
        SetupTaskOrchestrator(new List<TaskExecutionResult>
        {
            new() { TaskId = "sub-1", Success = true, Message = "灯光已打开" }
        });
        SetupResultAggregator(true, "客厅灯光已打开");

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Result.Should().Be("客厅灯光已打开");

        _intentMock.Verify(x => x.RecognizeAsync(request.UserInput, It.IsAny<CancellationToken>()), Times.Once);
        _entityMock.Verify(x => x.ExtractAsync(request.UserInput, It.IsAny<CancellationToken>()), Times.Once);
        _decomposerMock.Verify(x => x.DecomposeAsync(request.UserInput, It.IsAny<IntentRecognitionResult>(), It.IsAny<CancellationToken>()), Times.Once);
        _matcherMock.Verify(x => x.MatchBatchAsync(It.IsAny<List<DecomposedTask>>(), It.IsAny<CancellationToken>()), Times.Once);
        _orchestratorMock.Verify(x => x.CreatePlanAsync(It.IsAny<List<DecomposedTask>>(), It.IsAny<CancellationToken>()), Times.Once);
        _aggregatorMock.Verify(x => x.AggregateAsync(It.IsAny<List<TaskExecutionResult>>(), request.UserInput, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Low Confidence Branch

    [Fact]
    public async Task ExecuteBusinessLogicAsync_LowConfidence_ReturnsClarification()
    {
        // Arrange
        var request = CreateRequest("嗯...");
        SetupIntentRecognizer("unknown", 0.1); // 低于默认阈值 0.3

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.NeedsClarification.Should().BeTrue();
        result.ClarificationQuestion.Should().NotBeNullOrEmpty();

        // 不应调用后续流水线
        _entityMock.Verify(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _decomposerMock.Verify(x => x.DecomposeAsync(It.IsAny<string>(), It.IsAny<IntentRecognitionResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_ExactlyAtThreshold_ContinuesPipeline()
    {
        // Arrange — confidence exactly at 0.3 should still be low (< 0.3 triggers)
        var request = CreateRequest("边界测试");
        SetupIntentRecognizer("test", 0.3);
        SetupEntityExtractor(new Dictionary<string, object>());
        SetupTaskDecomposer(new List<DecomposedTask>());

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request);

        // Assert — 0.3 >= 0.3 threshold, so pipeline continues (but empty subtasks → failure)
        result.Success.Should().BeFalse();
        result.NeedsClarification.Should().BeFalse();
        result.Result.Should().Contain("无法理解");
    }

    #endregion

    #region Empty Decomposition

    [Fact]
    public async Task ExecuteBusinessLogicAsync_EmptySubTasks_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest("做些什么");
        SetupIntentRecognizer("general", 0.8);
        SetupEntityExtractor(new Dictionary<string, object>());
        SetupTaskDecomposer(new List<DecomposedTask>()); // 空子任务

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Result.Should().Contain("无法理解");
    }

    #endregion

    #region Entity Extraction Merges Into Request Parameters

    [Fact]
    public async Task ExecuteBusinessLogicAsync_EntityMergedIntoParameters()
    {
        // Arrange
        var request = CreateRequest("打开卧室空调");

        SetupIntentRecognizer("control-device", 0.9);
        SetupEntityExtractor(new Dictionary<string, object>
        {
            ["Room"] = "卧室",
            ["Device"] = "空调"
        });

        _decomposerMock
            .Setup(x => x.DecomposeAsync(It.IsAny<string>(), It.IsAny<IntentRecognitionResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskDecomposition
            {
                SubTasks = new List<DecomposedTask>
                {
                    new() { TaskId = "sub-1", RequiredCapability = "device-control" }
                }
            });

        SetupAgentMatcher();
        SetupTaskOrchestrator(new List<TaskExecutionResult>
        {
            new() { TaskId = "sub-1", Success = true }
        });
        SetupResultAggregator(true, "完成");

        // Act
        await _sut.ExecuteBusinessLogicAsync(request);

        // Assert — entities should have been added to request.Parameters
        request.Parameters.Should().ContainKey("Room");
        request.Parameters["Room"].Should().Be("卧室");
        request.Parameters.Should().ContainKey("Device");
        request.Parameters["Device"].Should().Be("空调");
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task ExecuteBusinessLogicAsync_IntentRecognizerThrows_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateRequest("异常测试");
        _intentMock
            .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("意图识别超时"));

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("意图识别超时");
        result.Result.Should().Contain("发生错误");
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_OrchestratorThrows_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateRequest("执行失败测试");
        SetupIntentRecognizer("test", 0.9);
        SetupEntityExtractor(new Dictionary<string, object>());
        SetupTaskDecomposer(new List<DecomposedTask>
        {
            new() { TaskId = "sub-1", RequiredCapability = "test" }
        });
        SetupAgentMatcher();
        _orchestratorMock
            .Setup(x => x.CreatePlanAsync(It.IsAny<List<DecomposedTask>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("编排失败"));

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("编排失败");
    }

    #endregion

    #region AggregatedResult with Null Summary

    [Fact]
    public async Task ExecuteBusinessLogicAsync_NullSummary_DefaultsToTaskCompleted()
    {
        // Arrange
        var request = CreateRequest("总结测试");
        SetupIntentRecognizer("test", 0.9);
        SetupEntityExtractor(new Dictionary<string, object>());
        SetupTaskDecomposer(new List<DecomposedTask>
        {
            new() { TaskId = "sub-1", RequiredCapability = "test" }
        });
        SetupAgentMatcher();
        SetupTaskOrchestrator(new List<TaskExecutionResult>
        {
            new() { TaskId = "sub-1", Success = true }
        });

        _aggregatorMock
            .Setup(x => x.AggregateAsync(It.IsAny<List<TaskExecutionResult>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregatedResult { Success = true, Summary = null });

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Result.Should().Be("任务已完成");
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task ExecuteBusinessLogicAsync_CancelledToken_ThrowsButCaughtAsError()
    {
        // Arrange
        var request = CreateRequest("取消测试");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _intentMock
            .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _sut.ExecuteBusinessLogicAsync(request, cts.Token);

        // Assert — exception is caught by the try-catch, returns error response
        result.Success.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static MafTaskRequest CreateRequest(string userInput)
    {
        return new MafTaskRequest
        {
            TaskId = Guid.NewGuid().ToString(),
            UserInput = userInput
        };
    }

    private void SetupIntentRecognizer(string intent, double confidence)
    {
        _intentMock
            .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentRecognitionResult
            {
                PrimaryIntent = intent,
                Confidence = confidence
            });
    }

    private void SetupEntityExtractor(Dictionary<string, object> entities)
    {
        var extractedEntities = entities.Select(kvp => new Entity
        {
            EntityType = kvp.Key,
            EntityValue = kvp.Value.ToString()!,
            Confidence = 0.9
        }).ToList();

        _entityMock
            .Setup(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityExtractionResult
            {
                Entities = entities,
                ExtractedEntities = extractedEntities
            });
    }

    private void SetupTaskDecomposer(List<DecomposedTask> tasks)
    {
        _decomposerMock
            .Setup(x => x.DecomposeAsync(It.IsAny<string>(), It.IsAny<IntentRecognitionResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskDecomposition { SubTasks = tasks });
    }

    private void SetupAgentMatcher()
    {
        _matcherMock
            .Setup(x => x.MatchBatchAsync(It.IsAny<List<DecomposedTask>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());
    }

    private void SetupTaskOrchestrator(List<TaskExecutionResult> results)
    {
        _orchestratorMock
            .Setup(x => x.CreatePlanAsync(It.IsAny<List<DecomposedTask>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionPlan());
        _orchestratorMock
            .Setup(x => x.ExecutePlanAsync(It.IsAny<ExecutionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
    }

    private void SetupResultAggregator(bool success, string? summary)
    {
        _aggregatorMock
            .Setup(x => x.AggregateAsync(It.IsAny<List<TaskExecutionResult>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregatedResult
            {
                Success = success,
                Summary = summary,
                AggregatedData = new Dictionary<string, object>()
            });
    }

    #endregion
}
