using CKY.MultiAgentFramework.Core.Models.Dialog;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models;

public class SlotDetectionResultTests
{
    [Fact]
    public void DefaultProperties_ShouldHaveCorrectDefaults()
    {
        var result = new SlotDetectionResult();

        result.Intent.Should().BeEmpty();
        result.MissingSlots.Should().NotBeNull().And.BeEmpty();
        result.OptionalSlots.Should().NotBeNull().And.BeEmpty();
        result.DetectedSlots.Should().NotBeNull().And.BeEmpty();
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public void AreRequiredSlotsFilled_NoMissingSlots_ReturnsTrue()
    {
        var result = new SlotDetectionResult { MissingSlots = new List<SlotDefinition>() };
        result.AreRequiredSlotsFilled().Should().BeTrue();
    }

    [Fact]
    public void AreRequiredSlotsFilled_HasMissingSlots_ReturnsFalse()
    {
        var result = new SlotDetectionResult
        {
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "location" }
            }
        };
        result.AreRequiredSlotsFilled().Should().BeFalse();
    }

    [Fact]
    public void GetSlotValue_ExistingSlot_ReturnsValue()
    {
        var result = new SlotDetectionResult();
        result.DetectedSlots["temperature"] = 25;

        result.GetSlotValue("temperature").Should().Be(25);
    }

    [Fact]
    public void GetSlotValue_NonExistingSlot_ReturnsNull()
    {
        var result = new SlotDetectionResult();
        result.GetSlotValue("nonexistent").Should().BeNull();
    }

    [Fact]
    public void SetSlotValue_ShouldAddOrUpdate()
    {
        var result = new SlotDetectionResult();

        result.SetSlotValue("color", "red");
        result.GetSlotValue("color").Should().Be("red");

        result.SetSlotValue("color", "blue");
        result.GetSlotValue("color").Should().Be("blue");
    }

    [Fact]
    public void GetMissingSlotNames_ShouldReturnNames()
    {
        var result = new SlotDetectionResult
        {
            MissingSlots = new List<SlotDefinition>
            {
                new() { SlotName = "location" },
                new() { SlotName = "device" }
            }
        };

        var names = result.GetMissingSlotNames();
        names.Should().BeEquivalentTo("location", "device");
    }

    [Fact]
    public void GetMissingSlotNames_NoMissing_ReturnsEmpty()
    {
        var result = new SlotDetectionResult();
        result.GetMissingSlotNames().Should().BeEmpty();
    }
}
