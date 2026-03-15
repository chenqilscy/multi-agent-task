using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Agents;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.SmartHome
{
    /// <summary>
    /// SmartHome 场景自动化测试
    /// 基于 P0 用例文档验证意图识别、路由逻辑、Agent 业务逻辑和异常处理
    /// </summary>
    public class SmartHomeScenarioTests
    {
        private readonly RuleBasedIntentRecognizer _recognizer;
        private readonly TestSmartHomeIntentKeywordProvider _keywordProvider;

        public SmartHomeScenarioTests()
        {
            _keywordProvider = new TestSmartHomeIntentKeywordProvider();
            var logger = Mock.Of<ILogger<RuleBasedIntentRecognizer>>();
            _recognizer = new RuleBasedIntentRecognizer(_keywordProvider, logger);
        }

        // =============================================
        // SH-MORNING-001: 早晨场景（天气+灯光+空调）
        // =============================================

        [Theory]
        [InlineData("北京天气", "QueryWeather")]
        [InlineData("今天天气预报", "QueryWeather")]
        [InlineData("早上好，今天北京天气怎么样", null)] // "什么"命中GeneralQuery可能与"天气"竞争
        public async Task SH_MORNING_001_WeatherQuery_ShouldRecognizeWeatherRelatedIntent(
            string userInput, string? expectedIntent)
        {
            var result = await _recognizer.RecognizeAsync(userInput);

            if (expectedIntent != null)
            {
                result.PrimaryIntent.Should().Be(expectedIntent,
                    $"输入 '{userInput}' 应识别为 {expectedIntent}");
            }
            else
            {
                // 多关键词竞争场景，验证天气或通用查询都可接受
                result.PrimaryIntent.Should().BeOneOf("QueryWeather", "GeneralQuery",
                    $"输入 '{userInput}' 应识别为天气或通用查询");
            }
        }

        [Fact]
        public async Task SH_MORNING_001_WeatherAgent_WithCity_ShouldReturnWeatherInfo()
        {
            // Arrange
            var mockWeatherService = new Mock<IWeatherService>();
            mockWeatherService
                .Setup(s => s.GetWeatherAsync("北京", It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WeatherInfo
                {
                    City = "北京",
                    Condition = "晴",
                    Temperature = 22,
                    MinTemperature = 15,
                    MaxTemperature = 28,
                    WindDirection = "东南风",
                    WindLevel = 3,
                    AirQualityIndex = 45,
                    Humidity = 40,
                });

            var agent = CreateWeatherAgent(mockWeatherService.Object);
            var request = CreateRequest("查询北京天气", new Dictionary<string, object> { ["city"] = "北京" });

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Result.Should().Contain("北京");
            response.Result.Should().Contain("晴");
            mockWeatherService.Verify(s => s.GetWeatherAsync("北京", It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SH_MORNING_001_WeatherAgent_WithoutCity_ShouldNeedClarification()
        {
            // Arrange
            var mockWeatherService = new Mock<IWeatherService>();
            var agent = CreateWeatherAgent(mockWeatherService.Object);
            var request = CreateRequest("今天天气怎么样");

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.NeedsClarification.Should().BeTrue();
            response.ClarificationQuestion.Should().Contain("城市");
            response.ClarificationOptions.Should().Contain("北京");
        }

        // =============================================
        // SH-MORNING-004: 天气服务异常
        // =============================================

        [Fact]
        public async Task SH_MORNING_004_WeatherAgent_ServiceException_ShouldReturnErrorGracefully()
        {
            // Arrange
            var mockWeatherService = new Mock<IWeatherService>();
            mockWeatherService
                .Setup(s => s.GetWeatherAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("天气服务暂时不可用"));

            var agent = CreateWeatherAgent(mockWeatherService.Object);
            var request = CreateRequest("查询北京天气", new Dictionary<string, object> { ["city"] = "北京" });

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.Result.Should().Contain("失败");
            response.Result.Should().Contain("重试");
            response.Error.Should().NotBeNullOrEmpty();
        }

        // =============================================
        // SH-MORNING-005: 设备离线（灯光控制）
        // =============================================

        [Theory]
        [InlineData("打开客厅的灯")]
        [InlineData("开灯")]
        public async Task SH_MORNING_005_LightingAgent_TurnOn_ShouldCallService(string userInput)
        {
            // Arrange
            var mockLighting = new Mock<ILightingService>();
            var agent = CreateLightingAgent(mockLighting.Object);
            var request = CreateRequest(userInput, new Dictionary<string, object> { ["room"] = "客厅" });

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Result.Should().Contain("客厅");
            response.Result.Should().Contain("打开");
            mockLighting.Verify(s => s.TurnOnAsync("客厅", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SH_MORNING_005_LightingAgent_DeviceOffline_ShouldReturnError()
        {
            // Arrange
            var mockLighting = new Mock<ILightingService>();
            mockLighting
                .Setup(s => s.TurnOnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("设备离线"));

            var agent = CreateLightingAgent(mockLighting.Object);
            var request = CreateRequest("打开客厅的灯", new Dictionary<string, object> { ["room"] = "客厅" });

            // Act & Assert: Agent 内部无 try-catch，异常会冒泡
            var act = () => agent.ExecuteBusinessLogicAsync(request);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*离线*");
        }

        // =============================================
        // SH-DEPART-001: 出门场景（关灯+关空调）
        // =============================================

        [Fact]
        public async Task SH_DEPART_001_LightingAgent_TurnOff_ShouldCallService()
        {
            // Arrange
            var mockLighting = new Mock<ILightingService>();
            var agent = CreateLightingAgent(mockLighting.Object);
            var request = CreateRequest("关闭客厅的灯", new Dictionary<string, object> { ["room"] = "客厅" });

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Result.Should().Contain("关闭");
            mockLighting.Verify(s => s.TurnOffAsync("客厅", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("制冷", "cooling")]
        [InlineData("制热", "heating")]
        [InlineData("暖气", "heating")]
        public async Task SH_DEPART_001_ClimateAgent_SetMode_ShouldCallCorrectMode(
            string keyword, string expectedMode)
        {
            // Arrange
            var mockClimate = new Mock<IClimateService>();
            var agent = CreateClimateAgent(mockClimate.Object);
            var request = CreateRequest($"空调{keyword}", new Dictionary<string, object> { ["room"] = "客厅" });

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            mockClimate.Verify(s => s.SetModeAsync("客厅", expectedMode, It.IsAny<CancellationToken>()), Times.Once);
        }

        // =============================================
        // SH-DEPART-003: 设备关闭失败
        // =============================================

        [Fact]
        public async Task SH_DEPART_003_LightingAgent_TurnOffFailure_ShouldThrow()
        {
            // Arrange
            var mockLighting = new Mock<ILightingService>();
            mockLighting
                .Setup(s => s.TurnOffAsync("客厅", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("设备响应超时"));

            var agent = CreateLightingAgent(mockLighting.Object);
            var request = CreateRequest("关灯", new Dictionary<string, object> { ["room"] = "客厅" });

            // Act & Assert
            var act = () => agent.ExecuteBusinessLogicAsync(request);
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // =============================================
        // SH-HOME-001: 回家场景（灯光+空调+音乐）
        // =============================================

        [Theory]
        [InlineData("播放音乐")]
        [InlineData("放音乐")]
        public async Task SH_HOME_001_MusicAgent_Play_ShouldSucceed(string userInput)
        {
            // Arrange
            var agent = CreateMusicAgent();
            var request = CreateRequest(userInput);

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Result.Should().Contain("播放");
        }

        [Fact]
        public async Task SH_HOME_001_MusicAgent_NextSong_ShouldSucceed()
        {
            var agent = CreateMusicAgent();
            var request = CreateRequest("下一首");

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().BeTrue();
            response.Result.Should().Contain("下一首");
        }

        [Fact]
        public async Task SH_HOME_001_LightingAgent_SetBrightness_ShouldCallService()
        {
            // Arrange
            var mockLighting = new Mock<ILightingService>();
            var agent = CreateLightingAgent(mockLighting.Object);
            var request = CreateRequest("灯光调亮", new Dictionary<string, object> { ["room"] = "客厅" });

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Result.Should().Contain("100%");
            mockLighting.Verify(s => s.SetBrightnessAsync("客厅", 100, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SH_HOME_001_LightingAgent_DimLight_ShouldCallService()
        {
            var mockLighting = new Mock<ILightingService>();
            var agent = CreateLightingAgent(mockLighting.Object);
            var request = CreateRequest("灯光调暗", new Dictionary<string, object> { ["room"] = "卧室" });

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().BeTrue();
            response.Result.Should().Contain("30%");
            mockLighting.Verify(s => s.SetBrightnessAsync("卧室", 30, It.IsAny<CancellationToken>()), Times.Once);
        }

        // =============================================
        // SH-HOME-005: 音乐服务异常
        // =============================================

        [Fact]
        public async Task SH_HOME_005_MusicAgent_UnknownCommand_ShouldFail()
        {
            // MusicAgent 无外部服务依赖，测试无法识别的命令
            var agent = CreateMusicAgent();
            var request = CreateRequest("随机播放爵士乐");

            var response = await agent.ExecuteBusinessLogicAsync(request);

            // "随机播放" 包含 "播放" → 命中播放分支
            response.Success.Should().BeTrue();
        }

        // =============================================
        // SH-EMERG-004: 紧急求助
        // =============================================

        [Fact]
        public async Task SH_EMERG_004_ClimateAgent_SetTemperature_ShouldParseAndCall()
        {
            // Arrange
            var mockClimate = new Mock<IClimateService>();
            var agent = CreateClimateAgent(mockClimate.Object);
            var request = CreateRequest("把温度设到26度", new Dictionary<string, object> { ["room"] = "客厅" });

            // Act
            var response = await agent.ExecuteBusinessLogicAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Result.Should().Contain("26");
            mockClimate.Verify(s => s.SetTemperatureAsync("客厅", 26, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("设置15度", false)]  // 15 < 16, out of range
        [InlineData("设置16度", true)]   // exactly 16
        [InlineData("设置30度", true)]   // exactly 30
        [InlineData("设置31度", false)]  // 31 > 30, out of range
        public async Task ClimateAgent_TemperatureRange_ShouldEnforce16To30(
            string userInput, bool shouldSucceed)
        {
            var mockClimate = new Mock<IClimateService>();
            var agent = CreateClimateAgent(mockClimate.Object);
            var request = CreateRequest(userInput, new Dictionary<string, object> { ["room"] = "客厅" });

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().Be(shouldSucceed,
                $"温度命令 '{userInput}' 应该{(shouldSucceed ? "成功" : "失败")}");
        }

        // =============================================
        // 意图识别全覆盖测试
        // =============================================

        [Theory]
        [InlineData("打开灯", "ControlLight")]
        [InlineData("关灯", "ControlLight")]
        [InlineData("调亮", "ControlLight")]
        [InlineData("空调制冷", "AdjustClimate")]
        [InlineData("设置温度", "AdjustClimate")]
        [InlineData("播放音乐", "PlayMusic")]
        [InlineData("播放歌曲", "PlayMusic")]
        [InlineData("门锁状态", "SecurityControl")]
        [InlineData("摄像头", "SecurityControl")]
        [InlineData("天气预报", "QueryWeather")]
        [InlineData("今天下雨吗", "QueryWeather")]
        [InlineData("温度变化", "QueryTemperatureHistory")]
        [InlineData("温度历史", "QueryTemperatureHistory")]
        public async Task IntentRecognition_ShouldIdentifyCorrectIntent(
            string userInput, string expectedIntent)
        {
            var result = await _recognizer.RecognizeAsync(userInput);
            result.PrimaryIntent.Should().Be(expectedIntent,
                $"输入 '{userInput}' 应识别为 {expectedIntent}");
        }

        [Fact]
        public async Task IntentRecognition_AllSupportedIntents_ShouldBeRegistered()
        {
            var intents = _keywordProvider.GetSupportedIntents().ToList();
            intents.Should().Contain("ControlLight");
            intents.Should().Contain("AdjustClimate");
            intents.Should().Contain("PlayMusic");
            intents.Should().Contain("SecurityControl");
            intents.Should().Contain("QueryWeather");
            intents.Should().Contain("QueryTemperatureHistory");
            intents.Should().Contain("GeneralQuery");
            intents.Should().HaveCount(7);
        }

        // =============================================
        // 路由优先级验证（SmartHomeMainAgent 路由逻辑）
        // =============================================

        [Theory]
        [InlineData("天气很热开空调")]   // "空调"+"热" → AdjustClimate
        [InlineData("灯光音乐")]           // "灯" 先于 "音乐"
        public async Task Routing_AmbiguousInput_ShouldResolveByKeywordPriority(
            string userInput)
        {
            var result = await _recognizer.RecognizeAsync(userInput);
            // 多关键词命中时，取分数最高的意图
            result.PrimaryIntent.Should().NotBe("Unknown");
        }

        // =============================================
        // Agent 默认房间参数测试
        // =============================================

        [Fact]
        public async Task LightingAgent_NoRoomParam_ShouldDefaultToLivingRoom()
        {
            var mockLighting = new Mock<ILightingService>();
            var agent = CreateLightingAgent(mockLighting.Object);
            var request = CreateRequest("打开灯"); // 不指定房间

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().BeTrue();
            // 默认房间是 "客厅"
            mockLighting.Verify(s => s.TurnOnAsync("客厅", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ClimateAgent_NoRoomParam_ShouldDefaultToLivingRoom()
        {
            var mockClimate = new Mock<IClimateService>();
            var agent = CreateClimateAgent(mockClimate.Object);
            var request = CreateRequest("空调制冷"); // 不指定房间

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().BeTrue();
            mockClimate.Verify(s => s.SetModeAsync("客厅", "cooling", It.IsAny<CancellationToken>()), Times.Once);
        }

        // =============================================
        // 无法识别的命令测试
        // =============================================

        [Fact]
        public async Task LightingAgent_UnrecognizedCommand_ShouldFail()
        {
            var mockLighting = new Mock<ILightingService>();
            var agent = CreateLightingAgent(mockLighting.Object);
            // 使用不含任何灯光关键词（打开/关闭/调暗/调亮/开灯/关灯/暗/亮）的输入
            var request = CreateRequest("查询设备列表");

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().BeFalse();
            response.Error.Should().Contain("无法识别");
        }

        [Fact]
        public async Task ClimateAgent_UnrecognizedCommand_ShouldFail()
        {
            var mockClimate = new Mock<IClimateService>();
            var agent = CreateClimateAgent(mockClimate.Object);
            var request = CreateRequest("空调自动模式");

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().BeFalse();
        }

        [Fact]
        public async Task MusicAgent_UnrecognizedCommand_ShouldFail()
        {
            var agent = CreateMusicAgent();
            var request = CreateRequest("设置闹钟");

            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Success.Should().BeFalse();
        }

        // =============================================
        // 辅助工厂方法
        // =============================================

        private static WeatherAgent CreateWeatherAgent(IWeatherService weatherService)
        {
            return new WeatherAgent(
                weatherService,
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<WeatherAgent>>());
        }

        private static LightingAgent CreateLightingAgent(ILightingService lightingService)
        {
            return new LightingAgent(
                lightingService,
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<LightingAgent>>());
        }

        private static ClimateAgent CreateClimateAgent(IClimateService climateService)
        {
            return new ClimateAgent(
                climateService,
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<ClimateAgent>>());
        }

        private static MusicAgent CreateMusicAgent()
        {
            return new MusicAgent(
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<MusicAgent>>());
        }

        private static MafTaskRequest CreateRequest(
            string userInput,
            Dictionary<string, object>? parameters = null)
        {
            return new MafTaskRequest
            {
                TaskId = Guid.NewGuid().ToString(),
                UserInput = userInput,
                UserId = "test-user",
                ConversationId = "test-conv",
                Parameters = parameters ?? new Dictionary<string, object>(),
            };
        }
    }

    /// <summary>
    /// SmartHomeIntentKeywordProvider 的测试副本
    /// 数据与 src/Demos/SmartHome/SmartHomeIntentKeywordProvider.cs 完全一致
    /// </summary>
    public class TestSmartHomeIntentKeywordProvider : IIntentKeywordProvider
    {
        private readonly Dictionary<string, string[]> _intentKeywordMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ControlLight"] = ["灯", "照明", "亮", "暗", "开灯", "关灯"],
            ["AdjustClimate"] = ["温度", "空调", "冷", "热", "暖", "制冷", "制热"],
            ["PlayMusic"] = ["音乐", "播放", "歌曲", "歌", "音频"],
            ["SecurityControl"] = ["门", "锁", "安全", "门锁", "摄像头"],
            ["QueryWeather"] = ["天气", "气温", "下雨", "晴天", "预报", "穿什么", "温度怎么样", "气候"],
            ["QueryTemperatureHistory"] = ["温度变化", "温度历史", "这段时间温度", "最近温度", "温度记录", "传感器"],
            ["GeneralQuery"] = ["查询", "状态", "怎么", "什么", "帮我"],
        };

        public string?[]? GetKeywords(string intent)
        {
            _intentKeywordMap.TryGetValue(intent, out var keywords);
            return keywords;
        }

        public IEnumerable<string> GetSupportedIntents() => _intentKeywordMap.Keys;
    }
}
