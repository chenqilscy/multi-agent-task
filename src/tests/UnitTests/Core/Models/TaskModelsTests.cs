using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models
{
    public class TaskModelsTests
    {
        // ========== TaskDependency ==========

        [Fact]
        public void TaskDependency_CheckSatisfied_MustComplete_WhenCompleted_ReturnsTrue()
        {
            var dep = new TaskDependency { Type = DependencyType.MustComplete };
            var task = new DecomposedTask { Status = MafTaskStatus.Completed };

            dep.CheckSatisfied(task).Should().BeTrue();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_MustComplete_WhenRunning_ReturnsFalse()
        {
            var dep = new TaskDependency { Type = DependencyType.MustComplete };
            var task = new DecomposedTask { Status = MafTaskStatus.Running };

            dep.CheckSatisfied(task).Should().BeFalse();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_MustSucceed_WhenCompletedAndSuccess_ReturnsTrue()
        {
            var dep = new TaskDependency { Type = DependencyType.MustSucceed };
            var task = new DecomposedTask
            {
                Status = MafTaskStatus.Completed,
                Result = new TaskExecutionResult { Success = true }
            };

            dep.CheckSatisfied(task).Should().BeTrue();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_MustSucceed_WhenCompletedButFailed_ReturnsFalse()
        {
            var dep = new TaskDependency { Type = DependencyType.MustSucceed };
            var task = new DecomposedTask
            {
                Status = MafTaskStatus.Completed,
                Result = new TaskExecutionResult { Success = false }
            };

            dep.CheckSatisfied(task).Should().BeFalse();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_MustStart_WhenRunning_ReturnsTrue()
        {
            var dep = new TaskDependency { Type = DependencyType.MustStart };
            var task = new DecomposedTask { Status = MafTaskStatus.Running };

            dep.CheckSatisfied(task).Should().BeTrue();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_MustStart_WhenCompleted_ReturnsTrue()
        {
            var dep = new TaskDependency { Type = DependencyType.MustStart };
            var task = new DecomposedTask { Status = MafTaskStatus.Completed };

            dep.CheckSatisfied(task).Should().BeTrue();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_MustStart_WhenPending_ReturnsFalse()
        {
            var dep = new TaskDependency { Type = DependencyType.MustStart };
            var task = new DecomposedTask { Status = MafTaskStatus.Pending };

            dep.CheckSatisfied(task).Should().BeFalse();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_DataDependency_WhenCompletedWithData_ReturnsTrue()
        {
            var dep = new TaskDependency { Type = DependencyType.DataDependency };
            var task = new DecomposedTask
            {
                Status = MafTaskStatus.Completed,
                Result = new TaskExecutionResult { Success = true, Data = "some data" }
            };

            dep.CheckSatisfied(task).Should().BeTrue();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_DataDependency_WhenCompletedWithoutData_ReturnsFalse()
        {
            var dep = new TaskDependency { Type = DependencyType.DataDependency };
            var task = new DecomposedTask
            {
                Status = MafTaskStatus.Completed,
                Result = new TaskExecutionResult { Success = true, Data = null }
            };

            dep.CheckSatisfied(task).Should().BeFalse();
        }

        [Fact]
        public void TaskDependency_CheckSatisfied_NullTask_ReturnsFalse()
        {
            var dep = new TaskDependency { Type = DependencyType.MustComplete };

            dep.CheckSatisfied(null!).Should().BeFalse();
        }

        [Fact]
        public void TaskDependency_DefaultProperties()
        {
            var dep = new TaskDependency();

            dep.DependsOnTaskId.Should().BeEmpty();
            dep.IsSatisfied.Should().BeFalse();
            dep.Condition.Should().BeNull();
        }

        // ========== TaskExecutionResult ==========

        [Fact]
        public void TaskExecutionResult_Duration_ShouldBeSettable()
        {
            var result = new TaskExecutionResult
            {
                StartedAt = new DateTime(2025, 1, 1, 10, 0, 0),
                CompletedAt = new DateTime(2025, 1, 1, 10, 0, 30),
                Duration = TimeSpan.FromSeconds(30)
            };

            result.Duration.Should().Be(TimeSpan.FromSeconds(30));
            result.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public void TaskExecutionResult_DefaultProperties()
        {
            var result = new TaskExecutionResult();

            result.TaskId.Should().BeEmpty();
            result.Success.Should().BeFalse();
            result.Message.Should().BeNull();
            result.Error.Should().BeNull();
            result.Data.Should().BeNull();
            result.Duration.Should().BeNull();
            result.RetryCount.Should().Be(0);
        }

        // ========== DecomposedTask ==========

        [Fact]
        public void DecomposedTask_DefaultProperties()
        {
            var task = new DecomposedTask();

            task.TaskId.Should().NotBeNullOrEmpty();
            task.TaskName.Should().BeEmpty();
            task.Status.Should().Be(MafTaskStatus.Pending);
            task.Dependencies.Should().NotBeNull().And.BeEmpty();
            task.Parameters.Should().NotBeNull().And.BeEmpty();
            task.Context.Should().NotBeNull().And.BeEmpty();
        }

        // ========== TaskGroup ==========

        [Fact]
        public void TaskGroup_DefaultProperties()
        {
            var group = new TaskGroup();

            group.GroupId.Should().NotBeNullOrEmpty();
            group.Tasks.Should().NotBeNull().And.BeEmpty();
        }

        // ========== ExecutionPlan ==========

        [Fact]
        public void ExecutionPlan_DefaultProperties()
        {
            var plan = new ExecutionPlan();

            plan.PlanId.Should().NotBeNullOrEmpty();
            plan.SerialGroups.Should().NotBeNull().And.BeEmpty();
            plan.ParallelGroups.Should().NotBeNull().And.BeEmpty();
        }

        // ========== TaskDecomposition ==========

        [Fact]
        public void TaskDecomposition_DefaultProperties()
        {
            var decomposition = new TaskDecomposition();

            decomposition.SubTasks.Should().NotBeNull().And.BeEmpty();
            decomposition.Metadata.Should().NotBeNull();
        }

        // ========== DecompositionMetadata ==========

        [Fact]
        public void DecompositionMetadata_DefaultProperties()
        {
            var meta = new DecompositionMetadata();

            meta.Strategy.Should().BeNull();
        }

        // ========== SubTaskResult ==========

        [Fact]
        public void SubTaskResult_CanSetAllProperties()
        {
            var result = new SubTaskResult
            {
                TaskId = "sub-1",
                Success = true,
                Message = "完成",
                Error = null
            };

            result.TaskId.Should().Be("sub-1");
            result.Success.Should().BeTrue();
            result.Message.Should().Be("完成");
            result.Error.Should().BeNull();
        }
    }
}
