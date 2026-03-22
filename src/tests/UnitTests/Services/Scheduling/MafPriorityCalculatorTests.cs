using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Scheduling;

public class MafPriorityCalculatorTests
{
    // Enum zero-values: Automatic=5, Deferred=3, Low=1 → baseline offset = 7
    private const int BaselineOffset = 5 + 3 - 1; // Automatic + Deferred - Low

    private readonly Mock<ILogger<MafPriorityCalculator>> _logger = new();

    private MafPriorityCalculator CreateSut() => new(_logger.Object);

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new MafPriorityCalculator(null!));
    }

    [Theory]
    [InlineData(TaskPriority.Critical, 40)]
    [InlineData(TaskPriority.High, 30)]
    [InlineData(TaskPriority.Normal, 20)]
    [InlineData(TaskPriority.Low, 10)]
    [InlineData(TaskPriority.Background, 5)]
    public void CalculatePriority_BasePriorityOnly_ShouldReturnCorrectBaseScore(
        TaskPriority priority, int expectedBase)
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = priority,
            UserInteraction = UserInteractionType.Automatic,  // 5
            TimeFactor = TimeFactor.Deferred,                 // 3
            ResourceUsage = ResourceUsage.Low                 // -1
        };

        var score = sut.CalculatePriority(request);
        score.Should().Be(expectedBase + BaselineOffset);
    }

    [Theory]
    [InlineData(UserInteractionType.Active, 30)]
    [InlineData(UserInteractionType.Passive, 15)]
    [InlineData(UserInteractionType.Automatic, 5)]
    public void CalculatePriority_UserInteraction_ShouldAddScore(
        UserInteractionType interaction, int expectedInteraction)
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Normal,               // 20
            UserInteraction = interaction,
            TimeFactor = TimeFactor.Deferred,                 // 3
            ResourceUsage = ResourceUsage.Low                 // -1
        };

        var score = sut.CalculatePriority(request);
        score.Should().Be(20 + expectedInteraction + 3 - 1);
    }

    [Theory]
    [InlineData(TimeFactor.Immediate, 15)]
    [InlineData(TimeFactor.Urgent, 12)]
    [InlineData(TimeFactor.Normal, 8)]
    [InlineData(TimeFactor.Deferred, 3)]
    public void CalculatePriority_TimeFactor_ShouldAddScore(TimeFactor factor, int expectedTime)
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Normal,               // 20
            UserInteraction = UserInteractionType.Automatic,  // 5
            TimeFactor = factor,
            ResourceUsage = ResourceUsage.Low                 // -1
        };

        var score = sut.CalculatePriority(request);
        score.Should().Be(20 + 5 + expectedTime - 1);
    }

    [Theory]
    [InlineData(ResourceUsage.High, 10)]
    [InlineData(ResourceUsage.Medium, 5)]
    [InlineData(ResourceUsage.Low, 1)]
    public void CalculatePriority_ResourceUsage_ShouldSubtractPenalty(
        ResourceUsage usage, int penalty)
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Normal,               // 20
            UserInteraction = UserInteractionType.Automatic,  // 5
            TimeFactor = TimeFactor.Deferred,                 // 3
            ResourceUsage = usage
        };

        var score = sut.CalculatePriority(request);
        score.Should().Be(20 + 5 + 3 - penalty);
    }

    [Fact]
    public void CalculatePriority_CriticalActiveImmediate_ShouldBeHigh()
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Critical,    // 40
            UserInteraction = UserInteractionType.Active,  // +30
            TimeFactor = TimeFactor.Immediate,       // +15
            ResourceUsage = ResourceUsage.Low        // -1
        };

        var score = sut.CalculatePriority(request);
        score.Should().Be(84);  // 40 + 30 + 15 - 1
    }

    [Fact]
    public void CalculatePriority_WithDependencyTask_ShouldAddDependencyBonus()
    {
        var sut = CreateSut();
        var depTask = new DecomposedTask { PriorityScore = 80 };
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Normal,               // 20
            UserInteraction = UserInteractionType.Automatic,  // 5
            TimeFactor = TimeFactor.Deferred,                 // 3
            ResourceUsage = ResourceUsage.Low,                // -1
            DependencyTask = depTask
        };

        var score = sut.CalculatePriority(request);
        // base=20+5+3-1=27, dependency bonus=(int)(80*0.05)=4
        score.Should().Be(31);
    }

    [Fact]
    public void CalculatePriority_WithOverdue_ShouldAddOverdueBonus()
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.High,               // 30
            UserInteraction = UserInteractionType.Active,   // +30
            TimeFactor = TimeFactor.Urgent,                  // +12
            ResourceUsage = ResourceUsage.Low,               // -1
            IsOverdue = true
        };

        var preOverdue = 30 + 30 + 12 - 1;       // 71
        var overdueBonus = (int)(preOverdue * 0.15); // 10
        var expected = preOverdue + overdueBonus;    // 81

        var score = sut.CalculatePriority(request);
        score.Should().Be(expected);
    }

    [Fact]
    public void CalculatePriority_ShouldClampToMax100()
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Critical,    // 40
            UserInteraction = UserInteractionType.Active,  // +30
            TimeFactor = TimeFactor.Immediate,       // +15
            ResourceUsage = ResourceUsage.Low,       // -1
            DependencyTask = new DecomposedTask { PriorityScore = 100 }, // +5
            IsOverdue = true  // +15% of 89 = 13 → 102 clamped to 100
        };

        var score = sut.CalculatePriority(request);
        score.Should().Be(100);
    }

    [Fact]
    public void CalculatePriority_ShouldClampToMin0()
    {
        var sut = CreateSut();
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Background,           // 5
            UserInteraction = UserInteractionType.Automatic,  // 5
            TimeFactor = TimeFactor.Deferred,                 // 3
            ResourceUsage = ResourceUsage.High                // -10
        };

        var score = sut.CalculatePriority(request);
        // 5 + 5 + 3 - 10 = 3 → still positive, clamped ≥ 0
        score.Should().Be(3);
    }
}
