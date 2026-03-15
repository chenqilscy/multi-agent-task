using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Services.Dialog;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.UnitTests.Services.Dialog
{
    public class SlotManagerTests
    {
        [Fact]
        public async Task DetectMissingSlots_WithPredefinedIntent_ReturnsCorrectSlots()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备名称" },
                    new SlotDefinition { SlotName = "Action", Description = "操作类型" },
                    new SlotDefinition { SlotName = "Location", Description = "位置" }
                }
            };
            mockProvider.Setup(x => x.GetDefinition("control_device")).Returns(slotDef);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "control_device" };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>
                {
                    ["Device"] = "空调",
                    ["Action"] = "打开"
                    // Location 缺失
                }
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("打开空调", intent, entities);

            // Assert
            Assert.Equal("control_device", result.Intent);
            Assert.Single(result.MissingSlots);  // Location
            Assert.Equal("Location", result.MissingSlots[0].SlotName);
            Assert.Equal(2, result.DetectedSlots.Count);
            Assert.True(result.DetectedSlots.ContainsKey("Device"));
            Assert.True(result.DetectedSlots.ContainsKey("Action"));

            var detectedSlotsCount = result.DetectedSlots.Count;
            var requiredSlotsCount = 3;
            var expectedConfidence = (double)detectedSlotsCount / requiredSlotsCount; // 2.0/3.0 = 0.67
            Assert.Equal(expectedConfidence, result.Confidence, 0.001);
        }

        [Fact]
        public async Task DetectMissingSlots_NoSlotDefinition_ReturnsZeroConfidence()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            mockProvider.Setup(x => x.GetDefinition("unknown_intent")).Returns((IntentSlotDefinition?)null);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "unknown_intent" };
            var entities = new EntityExtractionResult();

            // Act
            var result = await manager.DetectMissingSlotsAsync("unknown command", intent, entities);

            // Assert
            Assert.Equal("unknown_intent", result.Intent);
            Assert.Equal(0.0, result.Confidence);
        }

        [Fact]
        public async Task DetectMissingSlots_AllSlotsPresent_ReturnsFullConfidence()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备名称" },
                    new SlotDefinition { SlotName = "Action", Description = "操作类型" },
                    new SlotDefinition { SlotName = "Location", Description = "位置" }
                }
            };
            mockProvider.Setup(x => x.GetDefinition("control_device")).Returns(slotDef);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "control_device" };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>
                {
                    ["Device"] = "空调",
                    ["Action"] = "打开",
                    ["Location"] = "客厅"
                }
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("打开客厅空调", intent, entities);

            // Assert
            Assert.Empty(result.MissingSlots);
            Assert.Equal(3, result.DetectedSlots.Count);
            Assert.Equal(1.0, result.Confidence);
        }

        [Fact]
        public async Task DetectMissingSlots_NoRequiredSlots_ReturnsFullConfidence()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "simple_command",
                RequiredSlots = new() // No required slots
            };
            mockProvider.Setup(x => x.GetDefinition("simple_command")).Returns(slotDef);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "simple_command" };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>()
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("简单命令", intent, entities);

            // Assert
            Assert.Empty(result.MissingSlots);
            Assert.Equal(1.0, result.Confidence); // Edge case: no required slots = full confidence
        }

        [Fact]
        public async Task DetectMissingSlots_NoSlotsDetected_ReturnsZeroConfidence()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备名称" },
                    new SlotDefinition { SlotName = "Action", Description = "操作类型" },
                    new SlotDefinition { SlotName = "Location", Description = "位置" }
                }
            };
            mockProvider.Setup(x => x.GetDefinition("control_device")).Returns(slotDef);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "control_device" };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>() // No entities extracted
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("控制设备", intent, entities);

            // Assert
            Assert.Equal(3, result.MissingSlots.Count);
            Assert.Empty(result.DetectedSlots);
            Assert.Equal(0.0, result.Confidence);
        }

        [Fact]
        public async Task DetectMissingSlots_WithLogging_VerifiesLogCalls()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备名称" }
                }
            };
            mockProvider.Setup(x => x.GetDefinition("control_device")).Returns(slotDef);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "control_device" };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>()
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("打开空调", intent, entities);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Detecting missing slots")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DetectMissingSlots_WithUnknownIntent_UsesLlm()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            mockProvider.Setup(p => p.GetDefinition("unknown_intent")).Returns((IntentSlotDefinition?)null);

            // 使用测试专用的简单 Agent 实现
            var testAgent = new TestMafAiAgent();

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            mockRegistry.Setup(r => r.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(testAgent);

            var mockLogger = new Mock<ILogger<SlotManager>>();
            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "unknown_intent" };
            var entities = new EntityExtractionResult { Entities = new Dictionary<string, object>() };

            // Act
            var result = await manager.DetectMissingSlotsAsync("unknown request", intent, entities);

            // Assert
            mockRegistry.Verify(r => r.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal("unknown_intent", result.Intent);
            Assert.Equal(0.5, result.Confidence);
        }

        [Fact]
        public async Task FillSlots_WithDefaultValues_FillsMissingSlots()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition
                    {
                        SlotName = "Device",
                        Description = "设备",
                        Type = SlotType.String,
                        Required = true
                    }
                },
                OptionalSlots = new()
                {
                    new SlotDefinition
                    {
                        SlotName = "Mode",
                        Description = "模式",
                        Type = SlotType.Enumeration,
                        HasDefaultValue = true,
                        DefaultValue = "自动"
                    },
                    new SlotDefinition
                    {
                        SlotName = "Temperature",
                        Description = "温度",
                        Type = SlotType.Integer,
                        HasDefaultValue = true,
                        DefaultValue = 26
                    }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("control_device")).Returns(slotDef);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var providedSlots = new Dictionary<string, object>
            {
                ["Device"] = "空调"
            };
            var context = new DialogContext();

            // Act
            var result = await manager.FillSlotsAsync("control_device", providedSlots, context);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("空调", result["Device"]);
            Assert.Equal("自动", result["Mode"]);
            Assert.Equal(26, result["Temperature"]);
        }

        [Fact]
        public async Task FillSlots_WithHistoricalPreference_UsesHistoricalValue()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition
                    {
                        SlotName = "Device",
                        Description = "设备",
                        Type = SlotType.String,
                        Required = true
                    },
                    new SlotDefinition
                    {
                        SlotName = "Location",
                        Description = "位置",
                        Type = SlotType.String,
                        Required = true
                    }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("control_device")).Returns(slotDef);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var providedSlots = new Dictionary<string, object>
            {
                ["Device"] = "空调"
            };
            var context = new DialogContext
            {
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.Location"] = "客厅"
                }
            };

            // Act
            var result = await manager.FillSlotsAsync("control_device", providedSlots, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("空调", result["Device"]);
            Assert.Equal("客厅", result["Location"]);
        }

        [Fact]
        public async Task FillSlots_WithPreviousTurn_UsesPreviousSlotValue()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition
                    {
                        SlotName = "Device",
                        Description = "设备",
                        Type = SlotType.String,
                        Required = true
                    },
                    new SlotDefinition
                    {
                        SlotName = "Location",
                        Description = "位置",
                        Type = SlotType.String,
                        Required = true
                    }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("control_device")).Returns(slotDef);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var providedSlots = new Dictionary<string, object>
            {
                ["Device"] = "空调"
            };
            var context = new DialogContext
            {
                PreviousIntent = "control_device",
                PreviousSlots = new Dictionary<string, object>
                {
                    ["Device"] = "空调",
                    ["Location"] = "卧室"
                }
            };

            // Act
            var result = await manager.FillSlotsAsync("control_device", providedSlots, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("空调", result["Device"]);
            Assert.Equal("卧室", result["Location"]);
        }
    }

    /// <summary>
    /// 测试用的 MafAiAgent 实现
    /// </summary>
    internal class TestMafAiAgent : MafAiAgent
    {
        public TestMafAiAgent() : base(
            new LlmProviderConfig
            {
                ProviderName = "test",
                ModelId = "test-model",
                ApiKey = "test-key",
                ApiBaseUrl = "https://test.example.com",
                SupportedScenarios = new List<LlmScenario> { LlmScenario.Intent }
            },
            Mock.Of<ILogger<MafAiAgent>>())
        {
        }

        public override Task<string> ExecuteAsync(string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
        {
            // 返回模拟的 LLM 响应
            return Task.FromResult(@"{
                ""required_slots"": [
                    { ""name"": ""Location"", ""description"": ""城市"", ""provided"": false },
                    { ""name"": ""Date"", ""description"": ""日期"", ""provided"": true, ""value"": ""今天"" }
                ],
                ""confidence"": 0.5
            }");
        }

        public override IAsyncEnumerable<string> ExecuteStreamingAsync(string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
        {
            // 返回模拟的流式响应
            return AsyncEnumerable.Empty<string>();
        }
    }
}
