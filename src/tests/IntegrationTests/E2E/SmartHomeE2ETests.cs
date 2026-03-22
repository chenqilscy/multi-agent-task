using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using CKY.MultiAgentFramework.Demos.SmartHome.Services.Implementations;
using CKY.MultiAgentFramework.Infrastructure.Vectorization;
using CKY.MultiAgentFramework.Services.RAG;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.IntegrationTests.E2E;

/// <summary>
/// SmartHome Demo 端到端集成测试
/// 测试完整的Agent路由 → 业务逻辑 → 响应流程
/// </summary>
public class SmartHomeE2ETests
{
    private readonly SmartHomeControlService _controlService;
    private readonly Mock<IRagPipeline> _mockRagPipeline;

    public SmartHomeE2ETests()
    {
        var mockRegistry = new Mock<IMafAiAgentRegistry>();
        var mockDegradation = new Mock<IDegradationManager>();
        mockDegradation.Setup(d => d.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
        var mockRuleEngine = new Mock<IRuleEngine>();

        // 创建真实的模拟服务
        var lightingService = new SimulatedLightingService(NullLogger<SimulatedLightingService>.Instance);
        var climateService = new SimulatedClimateService(NullLogger<SimulatedClimateService>.Instance);
        var weatherService = new SimulatedWeatherService();
        var sensorService = new SimulatedSensorDataService();
        var securityService = new SimulatedSecurityService(NullLogger<SimulatedSecurityService>.Instance);

        // 创建RAG Pipeline Mock
        _mockRagPipeline = new Mock<IRagPipeline>();
        _mockRagPipeline.Setup(r => r.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponse
            {
                RetrievedChunks = [],
                UsedKnowledgeContext = false
            });

        // 创建Agent实例
        var lightingAgent = new LightingAgent(lightingService, mockRegistry.Object, NullLogger<LightingAgent>.Instance);
        var climateAgent = new ClimateAgent(climateService, mockRegistry.Object, NullLogger<ClimateAgent>.Instance);
        var musicAgent = new MusicAgent(mockRegistry.Object, NullLogger<MusicAgent>.Instance);
        var weatherAgent = new WeatherAgent(weatherService, mockRegistry.Object, NullLogger<WeatherAgent>.Instance);
        var temperatureHistoryAgent = new TemperatureHistoryAgent(sensorService, mockRegistry.Object, NullLogger<TemperatureHistoryAgent>.Instance);
        var securityAgent = new SecurityAgent(securityService, mockRegistry.Object, NullLogger<SecurityAgent>.Instance);
        var knowledgeBaseAgent = new KnowledgeBaseAgent(_mockRagPipeline.Object, mockRegistry.Object, NullLogger<KnowledgeBaseAgent>.Instance);

        _controlService = new SmartHomeControlService(
            lightingAgent, climateAgent, musicAgent, weatherAgent,
            temperatureHistoryAgent, securityAgent, knowledgeBaseAgent,
            mockDegradation.Object, mockRuleEngine.Object,
            NullLogger<SmartHomeControlService>.Instance);
    }

    [Theory]
    [InlineData("打开客厅的灯", true, "已打开")]
    [InlineData("关闭卧室的灯", true, "已关闭")]
    [InlineData("调暗书房的灯", true, "已调暗")]
    [InlineData("调亮灯", true, "已调亮")]
    public async Task LightingCommands_ShouldRouteToLightingAgent(string command, bool expectedSuccess, string expectedContains)
    {
        var result = await _controlService.ProcessCommandAsync(command);

        result.Success.Should().Be(expectedSuccess);
        result.Result.Should().Contain(expectedContains);
    }

    [Theory]
    [InlineData("把空调温度调到26度", true)]
    [InlineData("空调制冷模式", true)]
    public async Task ClimateCommands_ShouldRouteToClimateAgent(string command, bool expectedSuccess)
    {
        var result = await _controlService.ProcessCommandAsync(command);

        result.Success.Should().Be(expectedSuccess);
        result.Result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("播放轻音乐", true)]
    [InlineData("暂停音乐", true)]
    public async Task MusicCommands_ShouldRouteToMusicAgent(string command, bool expectedSuccess)
    {
        var result = await _controlService.ProcessCommandAsync(command);

        result.Success.Should().Be(expectedSuccess);
        result.Result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("今天北京天气怎么样", true)]
    [InlineData("明天上海会下雨吗", true)]
    public async Task WeatherCommands_ShouldRouteToWeatherAgent(string command, bool expectedSuccess)
    {
        var result = await _controlService.ProcessCommandAsync(command);

        result.Success.Should().Be(expectedSuccess);
        result.Result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("锁门", true)]
    [InlineData("查看摄像头", true)]
    [InlineData("开启外出模式", true)]
    public async Task SecurityCommands_ShouldRouteToSecurityAgent(string command, bool expectedSuccess)
    {
        var result = await _controlService.ProcessCommandAsync(command);

        result.Success.Should().Be(expectedSuccess);
        result.Result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task KnowledgeBaseCommand_ShouldRouteToKnowledgeBaseAgent()
    {
        var result = await _controlService.ProcessCommandAsync("知识库帮助");

        result.Success.Should().BeTrue();
        // 没有RAG数据时返回"没有找到相关内容"
        result.Result.Should().Contain("没有找到相关内容");
    }

    [Fact]
    public async Task KnowledgeBaseCommand_WithRagResults_ShouldReturnContent()
    {
        _mockRagPipeline.Setup(r => r.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponse
            {
                RetrievedChunks =
                [
                    new RetrievalResult { Content = "智能家居系统支持多种设备控制", Score = 0.9f, DocumentId = "doc1" }
                ],
                UsedKnowledgeContext = true
            });

        var result = await _controlService.ProcessCommandAsync("知识库 设备管理功能介绍");

        result.Success.Should().BeTrue();
        result.Result.Should().Contain("智能家居");
    }

    [Fact]
    public async Task UnrecognizedCommand_ShouldReturnError()
    {
        var result = await _controlService.ProcessCommandAsync("今天中午吃什么");

        result.Success.Should().BeFalse();
        result.Result.Should().Contain("无法理解");
    }

    [Fact]
    public async Task TemperatureHistory_ShouldRouteToTemperatureHistoryAgent()
    {
        var result = await _controlService.ProcessCommandAsync("客厅最近温度变化");

        result.Success.Should().BeTrue();
        result.Result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RuleEngine_Level5Degradation_ShouldUseRuleEngine()
    {
        // 设置降级Level 5
        var mockDegradation = new Mock<IDegradationManager>();
        mockDegradation.Setup(d => d.IsFeatureEnabled("llm")).Returns(false);
        mockDegradation.Setup(d => d.CurrentLevel).Returns(DegradationLevel.Level5);

        var mockRuleEngine = new Mock<IRuleEngine>();
        mockRuleEngine.Setup(r => r.CanHandle("打开灯")).Returns(true);
        mockRuleEngine.Setup(r => r.ProcessAsync(It.IsAny<MafTaskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MafTaskResponse { Success = true, Result = "规则引擎：灯已打开" });

        var mockRegistry = new Mock<IMafAiAgentRegistry>();
        var lightingService = new SimulatedLightingService(NullLogger<SimulatedLightingService>.Instance);
        var climateService = new SimulatedClimateService(NullLogger<SimulatedClimateService>.Instance);
        var weatherService = new SimulatedWeatherService();
        var sensorService = new SimulatedSensorDataService();
        var securityService = new SimulatedSecurityService(NullLogger<SimulatedSecurityService>.Instance);

        var controlService = new SmartHomeControlService(
            new LightingAgent(lightingService, mockRegistry.Object, NullLogger<LightingAgent>.Instance),
            new ClimateAgent(climateService, mockRegistry.Object, NullLogger<ClimateAgent>.Instance),
            new MusicAgent(mockRegistry.Object, NullLogger<MusicAgent>.Instance),
            new WeatherAgent(weatherService, mockRegistry.Object, NullLogger<WeatherAgent>.Instance),
            new TemperatureHistoryAgent(sensorService, mockRegistry.Object, NullLogger<TemperatureHistoryAgent>.Instance),
            new SecurityAgent(securityService, mockRegistry.Object, NullLogger<SecurityAgent>.Instance),
            new KnowledgeBaseAgent(_mockRagPipeline.Object, mockRegistry.Object, NullLogger<KnowledgeBaseAgent>.Instance),
            mockDegradation.Object, mockRuleEngine.Object,
            NullLogger<SmartHomeControlService>.Instance);

        var result = await controlService.ProcessCommandAsync("打开灯");

        result.Success.Should().BeTrue();
        result.Result.Should().Contain("规则引擎");
    }
}
