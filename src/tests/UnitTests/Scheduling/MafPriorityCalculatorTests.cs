using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Scheduling
{
    public class MafPriorityCalculatorTests
    {
        private readonly MafPriorityCalculator _sut;

        public MafPriorityCalculatorTests()
        {
            _sut = new MafPriorityCalculator(NullLogger<MafPriorityCalculator>.Instance);
        }

        [Fact]
        public void CalculatePriority_WithCriticalPriority_ReturnsHighScore()
        {
            // Arrange
            var request = new PriorityCalculationRequest
            {
                BasePriority = TaskPriority.Critical,
                UserInteraction = UserInteractionType.Active,
                TimeFactor = TimeFactor.Immediate
            };

            // Act
            var score = _sut.CalculatePriority(request);

            // Assert
            score.Should().BeGreaterThan(80);
        }

        [Theory]
        [InlineData(TaskPriority.Critical, UserInteractionType.Active, TimeFactor.Immediate, 80, 85)]  // 40+30+15-1=84
        [InlineData(TaskPriority.High, UserInteractionType.Active, TimeFactor.Immediate, 70, 75)]      // 30+30+15-1=74
        [InlineData(TaskPriority.Normal, UserInteractionType.Active, TimeFactor.Immediate, 60, 65)]    // 20+30+15-1=64
        [InlineData(TaskPriority.Low, UserInteractionType.Active, TimeFactor.Immediate, 50, 55)]       // 10+30+15-1=54
        public void CalculatePriority_VariousInputs_ReturnsExpectedScore(
            TaskPriority basePriority,
            UserInteractionType userInteraction,
            TimeFactor timeFactor,
            int minScore,
            int maxScore)
        {
            // Arrange
            var request = new PriorityCalculationRequest
            {
                BasePriority = basePriority,
                UserInteraction = userInteraction,
                TimeFactor = timeFactor
            };

            // Act
            var score = _sut.CalculatePriority(request);

            // Assert
            score.Should().BeInRange(minScore, maxScore);
        }

        [Fact]
        public void CalculatePriority_WithHighPriorityDependency_PropagatesScore()
        {
            // Arrange
            var dependency = new DecomposedTask { PriorityScore = 85 };
            var request = new PriorityCalculationRequest
            {
                BasePriority = TaskPriority.Normal,
                DependencyTask = dependency
            };

            // Act
            var score = _sut.CalculatePriority(request);

            // Assert
            score.Should().BeGreaterThan(20); // 大于Normal的基准分
            score.Should().BeLessThan(85);    // 不超过依赖任务
            // 依赖传播增加少量分数（85*0.05=4.25，限制为最大5分）
            // Normal(20) + Automatic(5) + Normal(8) - Low(1) + Dependency(4) = 36
            score.Should().Be(36);
        }

        [Fact]
        public void CalculatePriority_WhenOverdue_AddsBonus()
        {
            // Arrange
            var request = new PriorityCalculationRequest
            {
                BasePriority = TaskPriority.Normal,
                IsOverdue = true
            };

            // Act
            var score = _sut.CalculatePriority(request);

            // Assert
            var expectedBaseScore = 20; // Normal基础分
            score.Should().BeGreaterThan(expectedBaseScore);
        }
    }
}
