using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Scheduling
{
    public class MafTaskSchedulerTests
    {
        private readonly MafTaskScheduler _sut;
        private readonly MafPriorityCalculator _priorityCalculator;

        public MafTaskSchedulerTests()
        {
            _priorityCalculator = new MafPriorityCalculator(NullLogger<MafPriorityCalculator>.Instance);
            _sut = new MafTaskScheduler(_priorityCalculator, maxConcurrentTasks: 3);
        }

        [Fact]
        public async Task ScheduleAsync_WithMultipleTasks_CalculatesPriorities()
        {
            // Arrange
            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "1", TaskName = "Critical Task", Priority = TaskPriority.Critical },
                new() { TaskId = "2", TaskName = "Normal Task", Priority = TaskPriority.Normal },
                new() { TaskId = "3", TaskName = "Low Task", Priority = TaskPriority.Low }
            };

            // Act
            var result = await _sut.ScheduleAsync(tasks);

            // Assert
            result.ScheduledTasks.Should().HaveCount(3);
            result.ExecutionPlan.Should().NotBeNull();
        }

        [Fact]
        public async Task ScheduleAsync_SortsTasksByPriority()
        {
            // Arrange
            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "1", Priority = TaskPriority.Low },
                new() { TaskId = "2", Priority = TaskPriority.Critical },
                new() { TaskId = "3", Priority = TaskPriority.Normal }
            };

            // Act
            var result = await _sut.ScheduleAsync(tasks);

            // Assert
            result.ExecutionPlan.HighPriorityTasks.Should().HaveCount(1);
            result.ExecutionPlan.HighPriorityTasks[0].TaskId.Should().Be("2");
        }

        [Fact]
        public async Task ExecuteTaskAsync_WhenTaskSucceeds_ReturnsResult()
        {
            // Arrange
            var task = new DecomposedTask { TaskId = "test-1", TaskName = "Test Task" };
            var executed = false;

            // Act
            var result = await _sut.ExecuteTaskAsync(task, async (t, ct) =>
            {
                executed = true;
                return new TaskExecutionResult
                {
                    TaskId = t.TaskId,
                    Success = true,
                    Message = "Task completed"
                };
            });

            // Assert
            executed.Should().BeTrue();
            result.Success.Should().BeTrue();
            task.Status.Should().Be(MafTaskStatus.Completed);
        }

        [Fact]
        public async Task ExecuteTaskAsync_WhenTaskFails_UpdatesStatus()
        {
            // Arrange
            var task = new DecomposedTask { TaskId = "test-1", TaskName = "Failing Task" };

            // Act
            var result = await _sut.ExecuteTaskAsync(task, async (t, ct) =>
            {
                return new TaskExecutionResult
                {
                    TaskId = t.TaskId,
                    Success = false,
                    Error = "Task failed"
                };
            });

            // Assert
            result.Success.Should().BeFalse();
            task.Status.Should().Be(MafTaskStatus.Failed);
        }
    }
}
