using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;
using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using CKY.MultiAgentFramework.Services.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Resilience;

/// <summary>
/// 降级管理器和规则引擎单元测试
/// </summary>
public class DegradationAndRuleEngineTests
{
    #region DegradationManager 测试

    [Fact]
    public void DegradationManager_InitialLevel_ShouldBeNormal()
    {
        var manager = CreateDegradationManager();
        manager.CurrentLevel.Should().Be(DegradationLevel.Normal);
    }

    [Fact]
    public void DegradationManager_SetLevel_ShouldUpdateCurrentLevel()
    {
        var manager = CreateDegradationManager();
        manager.SetLevel(DegradationLevel.Level3);
        manager.CurrentLevel.Should().Be(DegradationLevel.Level3);
    }

    [Theory]
    [InlineData(DegradationLevel.Normal, "recommendations", true)]
    [InlineData(DegradationLevel.Level1, "recommendations", false)]
    [InlineData(DegradationLevel.Level1, "vector_search", true)]
    [InlineData(DegradationLevel.Level2, "vector_search", false)]
    [InlineData(DegradationLevel.Level2, "l2_cache", true)]
    [InlineData(DegradationLevel.Level3, "l2_cache", false)]
    [InlineData(DegradationLevel.Level3, "llm_premium", true)]
    [InlineData(DegradationLevel.Level4, "llm_premium", false)]
    [InlineData(DegradationLevel.Level4, "llm", true)]
    [InlineData(DegradationLevel.Level5, "llm", false)]
    public void DegradationManager_IsFeatureEnabled_ShouldReflectLevel(
        DegradationLevel level, string feature, bool expected)
    {
        var manager = CreateDegradationManager();
        manager.SetLevel(level);
        manager.IsFeatureEnabled(feature).Should().Be(expected);
    }

    [Fact]
    public void DegradationManager_UnknownFeature_ShouldBeEnabled()
    {
        var manager = CreateDegradationManager();
        manager.SetLevel(DegradationLevel.Level5);
        manager.IsFeatureEnabled("unknown_feature").Should().BeTrue();
    }

    #endregion

    #region SmartHomeRuleEngine 测试

    [Theory]
    [InlineData("打开客厅灯", true)]
    [InlineData("关灯", true)]
    [InlineData("开空调", true)]
    [InlineData("播放音乐", true)]
    [InlineData("锁门", true)]
    [InlineData("今天天气如何", false)]
    [InlineData("你好", false)]
    public void SmartHomeRuleEngine_CanHandle_ShouldMatchKeywords(string input, bool expected)
    {
        var engine = new SmartHomeRuleEngine();
        engine.CanHandle(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("打开灯", true, "打开灯光")]
    [InlineData("关灯", true, "关闭灯光")]
    [InlineData("调暗灯光", true, "调暗灯光")]
    [InlineData("调亮灯光", true, "调亮灯光")]
    [InlineData("开空调", true, "制冷模式")]
    [InlineData("制热", true, "制热模式")]
    [InlineData("播放音乐", true, "播放默认歌单")]
    [InlineData("锁门", true, "上锁")]
    [InlineData("解锁", true, "解锁")]
    [InlineData("外出模式", true, "外出安防")]
    public async Task SmartHomeRuleEngine_ProcessAsync_ShouldReturnExpected(
        string input, bool expectedSuccess, string resultContains)
    {
        var engine = new SmartHomeRuleEngine();
        var request = new MafTaskRequest { TaskId = "test-1", UserInput = input };

        var response = await engine.ProcessAsync(request);

        response.Success.Should().Be(expectedSuccess);
        response.Result.Should().Contain(resultContains);
        response.Result.Should().Contain("[规则引擎]");
    }

    [Fact]
    public async Task SmartHomeRuleEngine_UnknownInput_ShouldReturnFailure()
    {
        var engine = new SmartHomeRuleEngine();
        var request = new MafTaskRequest { TaskId = "test-2", UserInput = "讲个笑话" };

        var response = await engine.ProcessAsync(request);

        response.Success.Should().BeFalse();
        response.Result.Should().Contain("LLM服务不可用");
    }

    #endregion

    #region CustomerServiceRuleEngine 测试

    [Theory]
    [InlineData("查询订单", true)]
    [InlineData("我的快递呢", true)]
    [InlineData("退款", true)]
    [InlineData("投诉", true)]
    [InlineData("营业时间", true)]
    [InlineData("你叫什么名字", false)]
    public void CSRuleEngine_CanHandle_ShouldMatchKeywords(string input, bool expected)
    {
        var engine = new CustomerServiceRuleEngine();
        engine.CanHandle(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("查询我的订单", true, "订单号")]
    [InlineData("快递到哪了", true, "物流")]
    [InlineData("我要退款", true, "退款")]
    [InlineData("我要投诉", true, "反馈")]
    [InlineData("营业时间是什么", true, "9:00")]
    [InlineData("联系方式", true, "400")]
    [InlineData("退货政策", true, "7天")]
    public async Task CSRuleEngine_ProcessAsync_ShouldReturnExpected(
        string input, bool expectedSuccess, string resultContains)
    {
        var engine = new CustomerServiceRuleEngine();
        var request = new MafTaskRequest { TaskId = "cs-1", UserInput = input };

        var response = await engine.ProcessAsync(request);

        response.Success.Should().Be(expectedSuccess);
        response.Result.Should().Contain(resultContains);
        response.Result.Should().Contain("[规则引擎]");
    }

    [Fact]
    public async Task CSRuleEngine_UnknownInput_ShouldReturnFailure()
    {
        var engine = new CustomerServiceRuleEngine();
        var request = new MafTaskRequest { TaskId = "cs-2", UserInput = "给我算一道数学题" };

        var response = await engine.ProcessAsync(request);

        response.Success.Should().BeFalse();
        response.Result.Should().Contain("LLM服务不可用");
    }

    #endregion

    private static DegradationManager CreateDegradationManager()
    {
        return new DegradationManager(new Mock<ILogger<DegradationManager>>().Object);
    }
}
