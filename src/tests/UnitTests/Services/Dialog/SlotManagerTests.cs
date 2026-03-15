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

        [Fact]
        public async Task GenerateClarification_SingleMissingSlot_ReturnsDirectQuestion()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var missingSlots = new List<SlotDefinition>
            {
                new SlotDefinition
                {
                    SlotName = "Location",
                    Description = "位置",
                    Type = SlotType.String,
                    Required = true
                }
            };

            // Act
            var result = await manager.GenerateClarificationAsync(missingSlots, "control_device");

            // Assert
            Assert.Contains("位置", result);
            Assert.Contains("请问", result);
        }

        [Fact]
        public async Task GenerateClarification_SingleSlotWithValidValues_ShowsOptions()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var missingSlots = new List<SlotDefinition>
            {
                new SlotDefinition
                {
                    SlotName = "Action",
                    Description = "操作",
                    Type = SlotType.Enumeration,
                    Required = true,
                    ValidValues = new[] { "打开", "关闭", "调节" }
                }
            };

            // Act
            var result = await manager.GenerateClarificationAsync(missingSlots, "control_device");

            // Assert
            Assert.Contains("操作", result);
            Assert.Contains("打开", result);
            Assert.Contains("关闭", result);
            Assert.Contains("选择", result);
        }

        [Fact]
        public async Task GenerateClarification_MultipleMissingSlots_ListsAllSlots()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var missingSlots = new List<SlotDefinition>
            {
                new SlotDefinition { SlotName = "Device", Description = "设备", Required = true },
                new SlotDefinition { SlotName = "Action", Description = "操作", Required = true }
            };

            // Act
            var result = await manager.GenerateClarificationAsync(missingSlots, "control_device");

            // Assert
            Assert.Contains("设备", result);
            Assert.Contains("操作", result);
            Assert.Contains("请提供", result);
        }

        [Fact]
        public async Task GenerateClarification_LocationAndDateCombination_ReturnsCombinedQuestion()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var missingSlots = new List<SlotDefinition>
            {
                new SlotDefinition { SlotName = "Location", Description = "城市", Required = true },
                new SlotDefinition { SlotName = "Date", Description = "日期", Required = true }
            };

            // Act
            var result = await manager.GenerateClarificationAsync(missingSlots, "query_weather");

            // Assert
            Assert.Contains("城市", result);
            Assert.Contains("天气", result);
            Assert.Contains("哪个城市", result);
        }

        [Fact]
        public async Task GenerateClarification_DeviceLocationActionCombination_ReturnsCombinedQuestion()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var missingSlots = new List<SlotDefinition>
            {
                new SlotDefinition { SlotName = "Device", Description = "设备", Required = true },
                new SlotDefinition { SlotName = "Location", Description = "位置", Required = true },
                new SlotDefinition { SlotName = "Action", Description = "操作", Required = true }
            };

            // Act
            var result = await manager.GenerateClarificationAsync(missingSlots, "control_device");

            // Assert
            Assert.Contains("房间", result);
            Assert.Contains("设备", result);
        }

        [Fact]
        public async Task GenerateClarification_OptionalSlotsOnly_PromptsWithOptional()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var missingSlots = new List<SlotDefinition>
            {
                new SlotDefinition
                {
                    SlotName = "Mode",
                    Description = "模式",
                    Required = false,
                    ValidValues = new[] { "制冷", "制热", "除湿", "送风" }
                }
            };

            // Act
            var result = await manager.GenerateClarificationAsync(missingSlots, "control_device");

            // Assert
            Assert.Contains("模式", result);
            Assert.Contains("可选", result);
        }

        [Fact]
        public async Task GenerateClarification_EmptyList_ReturnsEmptyString()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var missingSlots = new List<SlotDefinition>();

            // Act
            var result = await manager.GenerateClarificationAsync(missingSlots, "control_device");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task FillSlots_HistoricalAndPreviousBothExist_HistoricalTakesPriority()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备", Required = true },
                    new SlotDefinition { SlotName = "Location", Description = "位置", Required = true }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("control_device")).Returns(slotDef);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var providedSlots = new Dictionary<string, object> { ["Device"] = "空调" };
            var context = new DialogContext
            {
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.Location"] = "客厅"  // 历史偏好
                },
                PreviousIntent = "control_device",
                PreviousSlots = new Dictionary<string, object>
                {
                    ["Device"] = "空调",
                    ["Location"] = "卧室"  // 上一轮值
                }
            };

            // Act
            var result = await manager.FillSlotsAsync("control_device", providedSlots, context);

            // Assert
            Assert.Equal("客厅", result["Location"]);  // 历史偏好优先
        }

        [Fact]
        public async Task FillSlots_PreviousIntentDifferent_DoesNotReuseSlots()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "query_weather",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Location", Description = "城市", Required = true }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("query_weather")).Returns(slotDef);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var providedSlots = new Dictionary<string, object>();
            var context = new DialogContext
            {
                PreviousIntent = "control_device",  // 不同的意图
                PreviousSlots = new Dictionary<string, object>
                {
                    ["Location"] = "卧室"
                }
            };

            // Act
            var result = await manager.FillSlotsAsync("query_weather", providedSlots, context);

            // Assert
            Assert.False(result.ContainsKey("Location"));  // 不应复用
        }

        [Fact]
        public async Task FillSlots_NullContext_DoesNotThrow()
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
                        Required = true,
                        HasDefaultValue = true,
                        DefaultValue = "空调"
                    }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("control_device")).Returns(slotDef);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var providedSlots = new Dictionary<string, object>();

            // Act & Assert
            var result = await manager.FillSlotsAsync("control_device", providedSlots, null!);

            Assert.Equal("空调", result["Device"]);  // 使用默认值
        }

        [Fact]
        public async Task FillSlots_EmptyHistoricalSlots_DoesNotThrow()
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
                        Required = true,
                        HasDefaultValue = true,
                        DefaultValue = "空调"
                    }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("control_device")).Returns(slotDef);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var providedSlots = new Dictionary<string, object>();
            var context = new DialogContext
            {
                HistoricalSlots = new Dictionary<string, object>()  // 空字典
            };

            // Act & Assert
            var result = await manager.FillSlotsAsync("control_device", providedSlots, context);

            Assert.Equal("空调", result["Device"]);  // 使用默认值
        }

        [Fact]
        public async Task DetectMissingSlots_LlmReturnsMalformedJson_ReturnsDefaultConfidence()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            mockProvider.Setup(p => p.GetDefinition("unknown")).Returns((IntentSlotDefinition?)null);

            var testAgent = new TestMafAiAgent(returnMalformedJson: true);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            mockRegistry.Setup(r => r.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(testAgent);

            var mockLogger = new Mock<ILogger<SlotManager>>();
            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "unknown" };
            var entities = new EntityExtractionResult { Entities = new Dictionary<string, object>() };

            // Act
            var result = await manager.DetectMissingSlotsAsync("test", intent, entities);

            // Assert
            Assert.Equal(0.5, result.Confidence);  // Default confidence when regex doesn't match
            Assert.Empty(result.MissingSlots);  // No slots parsed from malformed JSON
        }

        [Fact]
        public async Task DetectMissingSlots_LlmReturnsEmptyJson_ReturnsDefaultConfidence()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            mockProvider.Setup(p => p.GetDefinition("unknown")).Returns((IntentSlotDefinition?)null);

            var testAgent = new TestMafAiAgent(returnEmptyJson: true);

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            mockRegistry.Setup(r => r.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(testAgent);

            var mockLogger = new Mock<ILogger<SlotManager>>();
            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "unknown" };
            var entities = new EntityExtractionResult { Entities = new Dictionary<string, object>() };

            // Act
            var result = await manager.DetectMissingSlotsAsync("test", intent, entities);

            // Assert
            Assert.Equal(0.5, result.Confidence);  // Default confidence when regex doesn't match
            Assert.Empty(result.MissingSlots);  // No slots parsed from empty JSON
        }

        [Fact]
        public async Task DetectMissingSlots_WithContext_AutoFillsFromHistoricalSlots()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备" },
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
                    ["Device"] = "空调"
                    // Location 缺失，但应该从历史偏好中自动填充
                }
            };

            var context = new DialogContext
            {
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.Location"] = "客厅"
                }
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("打开空调", intent, entities, context);

            // Assert
            Assert.Empty(result.MissingSlots);  // Location 应该从历史偏好中自动填充
            Assert.Equal(2, result.DetectedSlots.Count);
            Assert.Equal("空调", result.DetectedSlots["Device"]);
            Assert.Equal("客厅", result.DetectedSlots["Location"]);
            Assert.Equal(1.0, result.Confidence);  // 所有槽位都已填充
        }

        [Fact]
        public async Task DetectMissingSlots_WithContext_AutoFillsFromPreviousTurn()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备" },
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
                    ["Device"] = "空调"
                    // Location 缺失，但应该从上一轮自动填充
                }
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
            var result = await manager.DetectMissingSlotsAsync("打开空调", intent, entities, context);

            // Assert
            Assert.Empty(result.MissingSlots);  // Location 应该从上一轮自动填充
            Assert.Equal(2, result.DetectedSlots.Count);
            Assert.Equal("空调", result.DetectedSlots["Device"]);
            Assert.Equal("卧室", result.DetectedSlots["Location"]);
            Assert.Equal(1.0, result.Confidence);
        }

        [Fact]
        public async Task DetectMissingSlots_WithContext_AutoFillsFromDefaultValue()
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
                        HasDefaultValue = true,
                        DefaultValue = "空调"
                    }
                }
            };
            mockProvider.Setup(x => x.GetDefinition("control_device")).Returns(slotDef);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "control_device" };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>()  // 没有提供任何实体
            };

            var context = new DialogContext();  // 空上下文，没有历史或上一轮数据

            // Act
            var result = await manager.DetectMissingSlotsAsync("控制设备", intent, entities, context);

            // Assert
            Assert.Empty(result.MissingSlots);  // Device 应该从默认值自动填充
            Assert.Single(result.DetectedSlots);
            Assert.Equal("空调", result.DetectedSlots["Device"]);
            Assert.Equal(1.0, result.Confidence);
        }

        [Fact]
        public async Task DetectMissingSlots_WithContext_HistoricalTakesPriorityOverPrevious()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备" },
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
                    ["Device"] = "空调"
                }
            };

            var context = new DialogContext
            {
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.Location"] = "客厅"  // 历史偏好
                },
                PreviousIntent = "control_device",
                PreviousSlots = new Dictionary<string, object>
                {
                    ["Device"] = "空调",
                    ["Location"] = "卧室"  // 上一轮值（应该被历史偏好覆盖）
                }
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("打开空调", intent, entities, context);

            // Assert
            Assert.Equal("客厅", result.DetectedSlots["Location"]);  // 历史偏好优先
            Assert.Equal(1.0, result.Confidence);
        }

        [Fact]
        public async Task DetectMissingSlots_WithContext_PreviousIntentDifferent_DoesNotReuse()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "query_weather",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Location", Description = "城市" }
                }
            };
            mockProvider.Setup(x => x.GetDefinition("query_weather")).Returns(slotDef);

            var mockLlmRegistry = new Mock<IMafAiAgentRegistry>();
            var mockLogger = new Mock<ILogger<SlotManager>>();

            var manager = new SlotManager(mockProvider.Object, mockLlmRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "query_weather" };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>()  // 没有提供 Location
            };

            var context = new DialogContext
            {
                PreviousIntent = "control_device",  // 不同的意图
                PreviousSlots = new Dictionary<string, object>
                {
                    ["Location"] = "卧室"
                }
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("查询天气", intent, entities, context);

            // Assert
            Assert.Single(result.MissingSlots);  // Location 不应该从上一轮自动填充（意图不同）
            Assert.False(result.DetectedSlots.ContainsKey("Location"));
        }

        [Fact]
        public async Task DetectMissingSlots_WithNullContext_WorksCorrectly()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备" },
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
                    ["Device"] = "空调"
                    // Location 缺失
                }
            };

            // Act - 传入 null context
            var result = await manager.DetectMissingSlotsAsync("打开空调", intent, entities, null!);

            // Assert
            Assert.Single(result.MissingSlots);  // Location 仍然缺失（没有上下文可以自动填充）
            Assert.Equal("Location", result.MissingSlots[0].SlotName);
            Assert.Single(result.DetectedSlots);
            Assert.Equal("空调", result.DetectedSlots["Device"]);
        }

        [Fact]
        public async Task DetectMissingSlots_UnknownIntentWithContext_AutoFillsFromHistorical()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            mockProvider.Setup(x => x.GetDefinition("unknown_intent")).Returns((IntentSlotDefinition?)null);

            var testAgent = new TestMafAiAgent();

            var mockRegistry = new Mock<IMafAiAgentRegistry>();
            mockRegistry.Setup(r => r.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(testAgent);

            var mockLogger = new Mock<ILogger<SlotManager>>();
            var manager = new SlotManager(mockProvider.Object, mockRegistry.Object, mockLogger.Object);

            var intent = new IntentRecognitionResult { PrimaryIntent = "unknown_intent" };
            var entities = new EntityExtractionResult { Entities = new Dictionary<string, object>() };

            var context = new DialogContext
            {
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["unknown_intent.Location"] = "北京"
                }
            };

            // Act
            var result = await manager.DetectMissingSlotsAsync("unknown request", intent, entities, context);

            // Assert
            mockRegistry.Verify(r => r.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal("unknown_intent", result.Intent);
            Assert.Equal("北京", result.DetectedSlots.GetValueOrDefault("Location"));  // 应该从历史自动填充
        }
    }

    /// <summary>
    /// 测试用的 MafAiAgent 实现
    /// </summary>
    internal class TestMafAiAgent : MafAiAgent
    {
        private readonly bool _returnMalformedJson;
        private readonly bool _returnEmptyJson;

        public TestMafAiAgent(bool returnMalformedJson = false, bool returnEmptyJson = false) : base(
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
            _returnMalformedJson = returnMalformedJson;
            _returnEmptyJson = returnEmptyJson;
        }

        public override Task<string> ExecuteAsync(string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
        {
            if (_returnMalformedJson)
            {
                return Task.FromResult("invalid json {{{");
            }

            if (_returnEmptyJson)
            {
                return Task.FromResult("{}");
            }

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
