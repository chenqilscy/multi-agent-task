using CKY.MultiAgentFramework.Core.Models.Dialog;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models;

public class DialogContextTests
{
    [Fact]
    public void DefaultProperties_ShouldBeCorrect()
    {
        var ctx = new DialogContext();

        ctx.SessionId.Should().BeEmpty();
        ctx.UserId.Should().BeEmpty();
        ctx.HistoricalSlots.Should().NotBeNull().And.BeEmpty();
        ctx.TurnCount.Should().Be(0);
        ctx.PreviousIntent.Should().BeNull();
        ctx.PreviousSlots.Should().BeNull();
        ctx.PendingClarification.Should().BeNull();
        ctx.PendingTask.Should().BeNull();
        ctx.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        ctx.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void HistoricalSlots_CanBeModified()
    {
        var ctx = new DialogContext();
        ctx.HistoricalSlots["control_device.Location"] = "客厅";

        ctx.HistoricalSlots.Should().ContainKey("control_device.Location");
    }

    [Fact]
    public void PendingClarificationInfo_DefaultProperties()
    {
        var info = new PendingClarificationInfo();

        info.Intent.Should().BeEmpty();
        info.DetectedSlots.Should().NotBeNull().And.BeEmpty();
        info.MissingSlots.Should().NotBeNull().And.BeEmpty();
        info.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PendingTaskInfo_DefaultProperties()
    {
        var info = new PendingTaskInfo();

        info.FilledSlots.Should().NotBeNull().And.BeEmpty();
        info.StillMissing.Should().NotBeNull().And.BeEmpty();
        info.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
