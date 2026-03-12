using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Orchestration
{
    public class MafTaskOrchestratorTests
    {
        private readonly MafTaskOrchestrator _sut;

        public MafTaskOrchestratorTests()
        {
            _sut = new MafTaskOrchestrator(NullLogger<MafTaskOrchestrator>.Instance);
        }

        [Fact]
        public async Task CreatePlanAsync_WithSingleTask_ShouldCreateSerialGroup()
        {
            // Arrange
            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "task1", TaskName = "打开灯", RequiredCapability = "lighting" }
            };

            // Act
            var plan = await _sut.CreatePlanAsync(tasks);

            // Assert
            plan.Should().NotBeNull();
            plan.PlanId.Should().NotBeNullOrEmpty();
            plan.SerialGroups.Should().HaveCount(1);
            plan.SerialGroups[0].Tasks.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreatePlanAsync_WithIndependentTasks_ShouldCreateParallelGroup()
        {
            // Arrange
            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "task1", TaskName = "打开灯", RequiredCapability = "lighting" },
                new() { TaskId = "task2", TaskName = "调节温度", RequiredCapability = "climate" }
            };

            // Act
            var plan = await _sut.CreatePlanAsync(tasks);

            // Assert
            plan.ParallelGroups.Should().HaveCount(1);
            plan.ParallelGroups[0].Tasks.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreatePlanAsync_WithDependentTasks_ShouldRespectOrder()
        {
            // Arrange
            var task1 = new DecomposedTask { TaskId = "task1", TaskName = "打开灯" };
            var task2 = new DecomposedTask
            {
                TaskId = "task2",
                TaskName = "调暗灯",
                Dependencies = new List<TaskDependency>
                {
                    new() { DependsOnTaskId = "task1", Type = DependencyType.MustComplete }
                }
            };
            var tasks = new List<DecomposedTask> { task1, task2 };

            // Act
            var plan = await _sut.CreatePlanAsync(tasks);

            // Assert
            var allGroups = plan.SerialGroups.Concat(plan.ParallelGroups).ToList();
            allGroups.Should().HaveCount(2);
        }

        [Fact]
        public async Task ExecutePlanAsync_ShouldReturnResults()
        {
            // Arrange
            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "task1", TaskName = "测试任务" }
            };
            var plan = await _sut.CreatePlanAsync(tasks);

            // Act
            var results = await _sut.ExecutePlanAsync(plan);

            // Assert
            results.Should().HaveCount(1);
            results[0].TaskId.Should().Be("task1");
            results[0].Success.Should().BeTrue();
        }
    }
}
