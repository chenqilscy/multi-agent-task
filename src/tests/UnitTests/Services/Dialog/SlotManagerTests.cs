using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
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
            Assert.Equal(0.67, result.Confidence, 0.01);  // 2/3
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
    }
}
