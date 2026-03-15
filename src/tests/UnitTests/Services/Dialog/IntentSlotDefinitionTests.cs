using CKY.MultiAgentFramework.Core.Models.Dialog;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.UnitTests.Core.Models.Dialog
{
    public class IntentSlotDefinitionTests
    {
        [Fact]
        public void GetAllSlots_ReturnsRequiredAndOptionalSlots()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "A", Required = true },
                    new SlotDefinition { SlotName = "B", Required = true }
                },
                OptionalSlots = new()
                {
                    new SlotDefinition { SlotName = "C", Required = false }
                }
            };

            // Act
            var allSlots = definition.GetAllSlots();

            // Assert
            Assert.Equal(3, allSlots.Count);
            Assert.Contains(allSlots, s => s.SlotName == "A");
            Assert.Contains(allSlots, s => s.SlotName == "B");
            Assert.Contains(allSlots, s => s.SlotName == "C");
        }

        [Fact]
        public void GetAllSlots_EmptyLists_ReturnsEmptyList()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new(),
                OptionalSlots = new()
            };

            // Act
            var allSlots = definition.GetAllSlots();

            // Assert
            Assert.Empty(allSlots);
        }

        [Fact]
        public void FindSlot_InRequiredSlots_ReturnsSlot()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Location", Required = true }
                }
            };

            // Act
            var slot = definition.FindSlot("Location");

            // Assert
            Assert.NotNull(slot);
            Assert.Equal("Location", slot!.SlotName);
        }

        [Fact]
        public void FindSlot_InOptionalSlots_ReturnsSlot()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                OptionalSlots = new()
                {
                    new SlotDefinition { SlotName = "Mode", Required = false }
                }
            };

            // Act
            var slot = definition.FindSlot("Mode");

            // Assert
            Assert.NotNull(slot);
            Assert.Equal("Mode", slot!.SlotName);
        }

        [Fact]
        public void FindSlot_NotFound_ReturnsNull()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new() { new SlotDefinition { SlotName = "A", Required = true } }
            };

            // Act
            var slot = definition.FindSlot("B");

            // Assert
            Assert.Null(slot);
        }

        [Fact]
        public void FindSlot_InBothRequiredAndOptional_ReturnsRequiredSlot()
        {
            // Arrange - 如果同一个槽位名同时出现在必填和可选中，应该返回必填的
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Required = true, Description = "必填设备" }
                },
                OptionalSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Required = false, Description = "可选设备" }
                }
            };

            // Act
            var slot = definition.FindSlot("Device");

            // Assert
            Assert.NotNull(slot);
            Assert.Equal("Device", slot!.SlotName);
            Assert.Equal("必填设备", slot.Description);  // 应该返回必填槽位的描述
        }

        [Fact]
        public void IsSlotRequired_RequiredSlot_ReturnsTrue()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Location", Required = true }
                }
            };

            // Act
            var result = definition.IsSlotRequired("Location");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSlotRequired_OptionalSlot_ReturnsFalse()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                OptionalSlots = new()
                {
                    new SlotDefinition { SlotName = "Mode", Required = false }
                }
            };

            // Act
            var result = definition.IsSlotRequired("Mode");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSlotRequired_NotFound_ReturnsFalse()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new() { new SlotDefinition { SlotName = "A", Required = true } }
            };

            // Act
            var result = definition.IsSlotRequired("B");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSlotRequired_EmptyLists_ReturnsFalse()
        {
            // Arrange
            var definition = new IntentSlotDefinition
            {
                Intent = "test",
                RequiredSlots = new(),
                OptionalSlots = new()
            };

            // Act
            var result = definition.IsSlotRequired("Any");

            // Assert
            Assert.False(result);
        }
    }
}
