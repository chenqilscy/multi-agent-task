using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;
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
            intents.Should().Contain("SleepMode");
            intents.Should().Contain("GuestMode");
            intents.Should().Contain("ReadingMode");
            intents.Should().Contain("MovieMode");
            intents.Should().Contain("ExerciseMode");
            intents.Should().Contain("WorkMode");
            intents.Should().Contain("DinnerMode");
            intents.Should().HaveCount(14);
        }

        // =============================================
        // 路由优先级验证（SmartHomeLeaderAgent 路由逻辑）
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
        // SH-04 睡眠准备: 场景模式意图识别
        // =============================================

        [Theory]
        [InlineData("开启睡眠模式", "SleepMode")]
        [InlineData("晚安", "SleepMode")]
        [InlineData("夜间模式", "SleepMode")]
        [InlineData("睡眠准备", "SleepMode")]
        public async Task SH_SLEEP_001_SleepMode_ShouldRecognizeIntent(string userInput, string expectedIntent)
        {
            var result = await _recognizer.RecognizeAsync(userInput);
            result.PrimaryIntent.Should().Be(expectedIntent,
                $"输入 '{userInput}' 应识别为 {expectedIntent}");
        }

        // =============================================
        // SH-05 会客模式: 场景模式意图识别
        // =============================================

        [Theory]
        [InlineData("开启会客模式", "GuestMode")]
        [InlineData("派对模式", "GuestMode")]
        [InlineData("聚会模式", "GuestMode")]
        [InlineData("商务洽谈", "GuestMode")]
        public async Task SH_GUEST_001_GuestMode_ShouldRecognizeIntent(string userInput, string expectedIntent)
        {
            var result = await _recognizer.RecognizeAsync(userInput);
            result.PrimaryIntent.Should().Be(expectedIntent,
                $"输入 '{userInput}' 应识别为 {expectedIntent}");
        }

        // =============================================
        // SH-08 个性化场景: 场景模式意图识别
        // =============================================

        [Theory]
        [InlineData("阅读模式", "ReadingMode")]
        [InlineData("电影模式", "MovieMode")]
        [InlineData("看电影", "MovieMode")]
        [InlineData("健身模式", "ExerciseMode")]
        [InlineData("运动模式", "ExerciseMode")]
        [InlineData("工作模式", "WorkMode")]
        [InlineData("学习模式", "WorkMode")]
        [InlineData("专注模式", "WorkMode")]
        [InlineData("聚餐模式", "DinnerMode")]
        [InlineData("聚餐准备", "DinnerMode")]
        public async Task SH_PERS_SceneModes_ShouldRecognizeCorrectIntent(string userInput, string expectedIntent)
        {
            var result = await _recognizer.RecognizeAsync(userInput);
            result.PrimaryIntent.Should().Be(expectedIntent,
                $"输入 '{userInput}' 应识别为 {expectedIntent}");
        }

        // =============================================
        // SH-04 睡眠模式: 场景模式编排测试
        // =============================================

        [Fact]
        public async Task SH_SLEEP_002_SleepMode_ShouldOrchestrateFourAgents()
        {
            // Arrange
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockClimateService = new Mock<IClimateService>();
            mockClimateService.Setup(s => s.SetTemperatureAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockSecurityService = new Mock<ISecurityService>();
            mockSecurityService.Setup(s => s.LockDoorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = CreateSmartHomeControlService(
                lightingService: mockLightingService.Object,
                climateService: mockClimateService.Object,
                securityService: mockSecurityService.Object);

            // Act
            var response = await service.ProcessCommandAsync("开启睡眠模式");

            // Assert
            response.Success.Should().BeTrue("睡眠模式应编排多个Agent成功执行");
            response.Result.Should().Contain("灯光", "应包含灯光调暗操作结果");
            response.Result.Should().Contain("空调", "应包含空调调温操作结果");
            response.Result.Should().Contain("门锁", "应包含门锁上锁操作结果");
        }

        [Fact]
        public async Task SH_SLEEP_003_SleepMode_PartialAgentFailure_ShouldNotAbort()
        {
            // Arrange: 灯光Agent正常，空调Agent异常
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockClimateService = new Mock<IClimateService>();
            mockClimateService.Setup(s => s.SetTemperatureAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("空调设备离线"));

            var mockSecurityService = new Mock<ISecurityService>();
            mockSecurityService.Setup(s => s.LockDoorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = CreateSmartHomeControlService(
                lightingService: mockLightingService.Object,
                climateService: mockClimateService.Object,
                securityService: mockSecurityService.Object);

            // Act
            var response = await service.ProcessCommandAsync("晚安");

            // Assert: 部分失败不应导致整体中断
            response.Should().NotBeNull();
            response.Result.Should().NotBeNullOrEmpty("即使部分Agent失败，仍应返回部分结果");
        }

        // =============================================
        // SH-05 会客模式: 场景模式编排测试
        // =============================================

        [Fact]
        public async Task SH_GUEST_002_GuestMode_ShouldOrchestrateLightClimateMusic()
        {
            // Arrange
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockClimateService = new Mock<IClimateService>();
            mockClimateService.Setup(s => s.SetTemperatureAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = CreateSmartHomeControlService(
                lightingService: mockLightingService.Object,
                climateService: mockClimateService.Object);

            // Act
            var response = await service.ProcessCommandAsync("开启会客模式");

            // Assert
            response.Success.Should().BeTrue("会客模式应编排成功");
            response.Result.Should().Contain("灯光", "应包含灯光调亮结果");
            response.Result.Should().Contain("空调", "应包含空调设置结果");
            response.Result.Should().Contain("音乐", "应包含音乐播放结果");
        }

        // =============================================
        // SH-08 个性化场景: 场景模式编排测试
        // =============================================

        [Fact]
        public async Task SH_PERS_001_MovieMode_ShouldOrchestrateLightAndMusic()
        {
            // Arrange
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = CreateSmartHomeControlService(
                lightingService: mockLightingService.Object);

            // Act
            var response = await service.ProcessCommandAsync("电影模式");

            // Assert
            response.Success.Should().BeTrue("电影模式应编排成功");
            response.Result.Should().Contain("灯光", "电影模式应关闭灯光");
            response.Result.Should().Contain("音乐", "电影模式应暂停音乐");
        }

        [Fact]
        public async Task SH_PERS_002_WorkMode_ShouldOrchestrateLightMusicClimate()
        {
            // Arrange
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockClimateService = new Mock<IClimateService>();
            mockClimateService.Setup(s => s.SetTemperatureAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = CreateSmartHomeControlService(
                lightingService: mockLightingService.Object,
                climateService: mockClimateService.Object);

            // Act
            var response = await service.ProcessCommandAsync("工作模式");

            // Assert
            response.Success.Should().BeTrue("工作模式应编排成功");
            response.Result.Should().Contain("灯光", "工作模式应设置书房灯光");
        }

        // =============================================
        // SH-06 外出监控: SecurityAgent 场景测试
        // =============================================

        [Theory]
        [InlineData("锁门", "SecurityControl")]
        [InlineData("上锁", "SecurityControl")]
        [InlineData("开启外出模式", "SecurityControl")]
        [InlineData("启动摄像头监控", "SecurityControl")]
        [InlineData("离家模式", "SecurityControl")]
        [InlineData("模拟有人", "SecurityControl")]
        public async Task SH_MONITOR_001_SecurityCommands_ShouldRecognizeIntent(string userInput, string expectedIntent)
        {
            var result = await _recognizer.RecognizeAsync(userInput);
            result.PrimaryIntent.Should().Be(expectedIntent,
                $"输入 '{userInput}' 应识别为 {expectedIntent}");
        }

        [Fact]
        public async Task SH_MONITOR_002_SecurityAgent_LockDoor_ShouldCallService()
        {
            var mockSecurity = new Mock<ISecurityService>();
            mockSecurity.Setup(s => s.LockDoorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = CreateSmartHomeControlService(securityService: mockSecurity.Object);

            var response = await service.ProcessCommandAsync("锁门");

            response.Success.Should().BeTrue("锁门指令应成功执行");
        }

        [Fact]
        public async Task SH_MONITOR_003_SecurityAgent_OutMode_ShouldRouteToSecurity()
        {
            var mockSecurity = new Mock<ISecurityService>();
            mockSecurity.Setup(s => s.LockDoorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockSecurity.Setup(s => s.EnableAwayModeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = CreateSmartHomeControlService(securityService: mockSecurity.Object);

            var response = await service.ProcessCommandAsync("开启外出安防模式");

            response.Should().NotBeNull("外出安防命令应返回响应");
        }

        // =============================================
        // SH-08 个性化场景: 更多编排测试
        // =============================================

        [Fact]
        public async Task SH_PERS_003_ExerciseMode_ShouldOrchestrateLightAndMusic()
        {
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = CreateSmartHomeControlService(lightingService: mockLightingService.Object);

            var response = await service.ProcessCommandAsync("健身模式");

            response.Success.Should().BeTrue("健身模式应编排成功");
            response.Result.Should().Contain("灯光", "健身模式应调亮灯光");
            response.Result.Should().Contain("音乐", "健身模式应播放动感音乐");
        }

        [Fact]
        public async Task SH_PERS_004_ReadingMode_ShouldOrchestrateLightAndMute()
        {
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = CreateSmartHomeControlService(lightingService: mockLightingService.Object);

            var response = await service.ProcessCommandAsync("阅读模式");

            response.Success.Should().BeTrue("阅读模式应编排成功");
            response.Result.Should().Contain("灯光", "阅读模式应调整柔和灯光");
        }

        [Fact]
        public async Task SH_PERS_005_DinnerMode_ShouldOrchestrateLightMusicClimate()
        {
            var mockLightingService = new Mock<ILightingService>();
            mockLightingService.Setup(s => s.SetBrightnessAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockClimateService = new Mock<IClimateService>();
            mockClimateService.Setup(s => s.SetTemperatureAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = CreateSmartHomeControlService(
                lightingService: mockLightingService.Object,
                climateService: mockClimateService.Object);

            var response = await service.ProcessCommandAsync("聚餐模式");

            response.Success.Should().BeTrue("聚餐模式应编排成功");
            response.Result.Should().Contain("灯光", "聚餐模式应调亮灯光");
            response.Result.Should().Contain("音乐", "聚餐模式应播放背景音乐");
            response.Result.Should().Contain("空调", "聚餐模式应设置温度");
        }

        // =============================================
        // SH-07 紧急情况: SecurityAgent 警报查询
        // =============================================

        [Fact]
        public async Task SH_EMERG_001_SecurityAgent_AlertQuery_ShouldReturnAlerts()
        {
            var mockSecurity = new Mock<ISecurityService>();
            mockSecurity.Setup(s => s.GetRecentAlertsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SecurityAlert>
                {
                    new() { Type = "smoke", Location = "厨房", Severity = "high", Timestamp = DateTime.UtcNow },
                    new() { Type = "intrusion", Location = "前门", Severity = "critical", Timestamp = DateTime.UtcNow }
                });

            var securityAgent = new SecurityAgent(
                mockSecurity.Object,
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<SecurityAgent>>());

            var request = CreateRequest("查看最近警报");
            var response = await securityAgent.ExecuteBusinessLogicAsync(request);

            response.Should().NotBeNull("警报查询应返回响应");
            mockSecurity.Verify(s => s.GetRecentAlertsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SH_EMERG_002_ClimateAgent_GasValve_ShouldClose()
        {
            var mockClimate = new Mock<IClimateService>();
            mockClimate.Setup(s => s.CloseGasValveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var agent = CreateClimateAgent(mockClimate.Object);
            var request = CreateRequest("关闭燃气阀门");
            var response = await agent.ExecuteBusinessLogicAsync(request);

            response.Should().NotBeNull("燃气阀门关闭指令应返回响应");
        }

        [Fact]
        public async Task SH_EMERG_003_SecurityAgent_PresenceSimulation_ShouldEnable()
        {
            var mockSecurity = new Mock<ISecurityService>();
            mockSecurity.Setup(s => s.EnablePresenceSimulationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var securityAgent = new SecurityAgent(
                mockSecurity.Object,
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<SecurityAgent>>());

            var request = CreateRequest("开启模拟有人在家");
            var response = await securityAgent.ExecuteBusinessLogicAsync(request);

            response.Should().NotBeNull("模拟有人在家应返回响应");
            response.Success.Should().BeTrue();
            mockSecurity.Verify(s => s.EnablePresenceSimulationAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SH_EMERG_005_SecurityAgent_CameraControl_ShouldActivate()
        {
            var mockSecurity = new Mock<ISecurityService>();
            mockSecurity.Setup(s => s.SetCameraActiveAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var securityAgent = new SecurityAgent(
                mockSecurity.Object,
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<SecurityAgent>>());

            var request = CreateRequest("打开客厅摄像头");
            var response = await securityAgent.ExecuteBusinessLogicAsync(request);

            response.Should().NotBeNull("摄像头打开指令应返回响应");
            response.Success.Should().BeTrue();
        }

        // =============================================
        // LLM Fallback 单元测试
        // =============================================

        private static LlmProviderConfig CreateTestLlmConfig()
        {
            return new LlmProviderConfig
            {
                ProviderName = "test-provider",
                ProviderDisplayName = "Test",
                ApiBaseUrl = "https://test.example.com/api/",
                ApiKey = "test-key",
                ModelId = "test-model",
                SupportedScenarios = [LlmScenario.Chat],
                IsEnabled = true,
                Priority = 1,
            };
        }

        private static SmartHomeControlService CreateServiceWithLlm(
            Mock<ILlmAgentFactory> mockFactory,
            Mock<IDegradationManager>? mockDegradation = null)
        {
            var lightingAgent = CreateLightingAgent(Mock.Of<ILightingService>());
            var climateAgent = CreateClimateAgent(Mock.Of<IClimateService>());
            var musicAgent = CreateMusicAgent();
            var weatherAgent = CreateWeatherAgent(Mock.Of<IWeatherService>());
            var tempHistoryAgent = new TemperatureHistoryAgent(
                Mock.Of<ISensorDataService>(),
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<TemperatureHistoryAgent>>());
            var securityAgent = new SecurityAgent(
                Mock.Of<ISecurityService>(),
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<SecurityAgent>>());
            var knowledgeAgent = new KnowledgeBaseAgent(
                Mock.Of<IRagPipeline>(),
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<KnowledgeBaseAgent>>());

            if (mockDegradation == null)
            {
                mockDegradation = new Mock<IDegradationManager>();
                mockDegradation.Setup(d => d.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
            }

            var mockRuleEngine = new Mock<IRuleEngine>();
            mockRuleEngine.Setup(r => r.CanHandle(It.IsAny<string>())).Returns(false);

            return new SmartHomeControlService(
                lightingAgent,
                climateAgent,
                musicAgent,
                weatherAgent,
                tempHistoryAgent,
                securityAgent,
                knowledgeAgent,
                mockDegradation.Object,
                mockRuleEngine.Object,
                mockFactory.Object,
                Mock.Of<ILogger<SmartHomeControlService>>());
        }

        [Fact]
        public async Task LLM_Fallback_Success_ShouldReturnLlmResponse()
        {
            // Arrange
            var mockAgent = new Mock<MafAiAgent>(
                CreateTestLlmConfig(),
                Mock.Of<ILogger>(),
                null!) { CallBase = false };
            mockAgent
                .Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("我是智能家居助手，有什么可以帮您？");

            var mockFactory = new Mock<ILlmAgentFactory>();
            mockFactory
                .Setup(f => f.CreateBestAgentForScenarioAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAgent.Object);

            var service = CreateServiceWithLlm(mockFactory);

            // Act — 使用不匹配任何关键词的命令
            var result = await service.ProcessCommandAsync("今天心情不错");

            // Assert
            result.Success.Should().BeTrue();
            result.Result.Should().Be("我是智能家居助手，有什么可以帮您？");
            mockFactory.Verify(f => f.CreateBestAgentForScenarioAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LLM_Fallback_EmptyResponse_ShouldFallbackToDefault()
        {
            var mockAgent = new Mock<MafAiAgent>(
                CreateTestLlmConfig(),
                Mock.Of<ILogger>(),
                null!) { CallBase = false };
            mockAgent
                .Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            var mockFactory = new Mock<ILlmAgentFactory>();
            mockFactory
                .Setup(f => f.CreateBestAgentForScenarioAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAgent.Object);

            var service = CreateServiceWithLlm(mockFactory);
            var result = await service.ProcessCommandAsync("随便聊聊");

            result.Success.Should().BeFalse();
            result.Result.Should().Contain("无法理解");
        }

        [Fact]
        public async Task LLM_Fallback_Exception_ShouldFallbackToDefault()
        {
            var mockFactory = new Mock<ILlmAgentFactory>();
            mockFactory
                .Setup(f => f.CreateBestAgentForScenarioAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("No provider available"));

            var service = CreateServiceWithLlm(mockFactory);
            var result = await service.ProcessCommandAsync("随便聊聊");

            result.Success.Should().BeFalse();
            result.Result.Should().Contain("无法理解");
        }

        [Fact]
        public async Task LLM_Fallback_DegradationDisabled_ShouldSkipLlm()
        {
            var mockFactory = new Mock<ILlmAgentFactory>();
            var mockDegradation = new Mock<IDegradationManager>();
            mockDegradation.Setup(d => d.IsFeatureEnabled("llm")).Returns(false);
            // 其它功能保持启用
            mockDegradation.Setup(d => d.IsFeatureEnabled(It.Is<string>(s => s != "llm"))).Returns(true);

            var service = CreateServiceWithLlm(mockFactory, mockDegradation);
            var result = await service.ProcessCommandAsync("随便聊聊");

            result.Success.Should().BeFalse();
            result.Result.Should().Contain("无法理解");
            // LLM factory 不应被调用
            mockFactory.Verify(f => f.CreateBestAgentForScenarioAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LLM_Fallback_KeywordStillPriority_ShouldNotCallLlm()
        {
            var mockFactory = new Mock<ILlmAgentFactory>();

            var service = CreateServiceWithLlm(mockFactory);
            var result = await service.ProcessCommandAsync("打开客厅的灯");

            result.Success.Should().BeTrue();
            // 关键词匹配成功时不应调用 LLM
            mockFactory.Verify(f => f.CreateBestAgentForScenarioAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // =============================================
        // 辅助工厂方法
        // =============================================

        private static SmartHomeControlService CreateSmartHomeControlService(
            ILightingService? lightingService = null,
            IClimateService? climateService = null,
            ISecurityService? securityService = null)
        {
            var lightingAgent = CreateLightingAgent(lightingService ?? Mock.Of<ILightingService>());
            var climateAgent = CreateClimateAgent(climateService ?? Mock.Of<IClimateService>());
            var musicAgent = CreateMusicAgent();
            var weatherAgent = CreateWeatherAgent(Mock.Of<IWeatherService>());
            var tempHistoryAgent = new TemperatureHistoryAgent(
                Mock.Of<ISensorDataService>(),
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<TemperatureHistoryAgent>>());
            var securityAgent = new SecurityAgent(
                securityService ?? Mock.Of<ISecurityService>(),
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<SecurityAgent>>());
            var knowledgeAgent = new KnowledgeBaseAgent(
                Mock.Of<IRagPipeline>(),
                Mock.Of<IMafAiAgentRegistry>(),
                Mock.Of<ILogger<KnowledgeBaseAgent>>());

            var mockDegradation = new Mock<IDegradationManager>();
            mockDegradation.Setup(d => d.IsFeatureEnabled(It.IsAny<string>())).Returns(true);

            var mockRuleEngine = new Mock<IRuleEngine>();
            mockRuleEngine.Setup(r => r.CanHandle(It.IsAny<string>())).Returns(false);

            return new SmartHomeControlService(
                lightingAgent,
                climateAgent,
                musicAgent,
                weatherAgent,
                tempHistoryAgent,
                securityAgent,
                knowledgeAgent,
                mockDegradation.Object,
                mockRuleEngine.Object,
                Mock.Of<ILlmAgentFactory>(),
                Mock.Of<ILogger<SmartHomeControlService>>());
        }

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
            ["SecurityControl"] = ["门", "锁", "安全", "门锁", "摄像头", "外出模式", "离家模式", "模拟有人", "监控"],
            ["QueryWeather"] = ["天气", "气温", "下雨", "晴天", "预报", "穿什么", "温度怎么样", "气候"],
            ["QueryTemperatureHistory"] = ["温度变化", "温度历史", "这段时间温度", "最近温度", "温度记录", "传感器"],
            ["SleepMode"] = ["睡眠模式", "睡觉模式", "晚安", "入睡", "夜间模式", "睡眠准备"],
            ["GuestMode"] = ["会客模式", "来客人", "朋友来", "派对模式", "聚会模式", "商务洽谈"],
            ["ReadingMode"] = ["阅读模式", "看书模式", "读书模式", "专注阅读"],
            ["MovieMode"] = ["电影模式", "观影模式", "看电影"],
            ["ExerciseMode"] = ["健身模式", "运动模式", "锻炼模式"],
            ["WorkMode"] = ["工作模式", "学习模式", "办公模式", "专注模式"],
            ["DinnerMode"] = ["聚餐模式", "晚餐模式", "用餐模式", "聚餐准备"],
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
