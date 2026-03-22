using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using FluentAssertions;
using RagQueryRequest = CKY.MultiAgentFramework.Core.Models.RAG.RagQueryRequest;

namespace CKY.MAF.Tests.Core.Models;

/// <summary>
/// 扩展的任务模型测试，覆盖 DecomposedTask.IsBlocked、MainTask 导航属性、
/// ResourceRequirements 全字段、DecompositionMetadata 赋值
/// </summary>
public class TaskModelExtendedTests
{
    // === DecomposedTask.IsBlocked ===

    [Fact]
    public void DecomposedTask_IsBlocked_NoDependencies_ReturnsFalse()
    {
        var task = new DecomposedTask();
        task.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void DecomposedTask_IsBlocked_AllSatisfied_ReturnsFalse()
    {
        var task = new DecomposedTask();
        task.Dependencies.Add(new TaskDependency { IsSatisfied = true });
        task.Dependencies.Add(new TaskDependency { IsSatisfied = true });
        task.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void DecomposedTask_IsBlocked_OneUnsatisfied_ReturnsTrue()
    {
        var task = new DecomposedTask();
        task.Dependencies.Add(new TaskDependency { IsSatisfied = true });
        task.Dependencies.Add(new TaskDependency { IsSatisfied = false });
        task.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public void DecomposedTask_SetAllProperties()
    {
        var now = DateTime.UtcNow;
        var task = new DecomposedTask
        {
            TaskId = "t-1",
            TaskName = "测试任务",
            Intent = "CodeGen",
            Description = "描述",
            Priority = TaskPriority.High,
            PriorityScore = 85,
            PriorityReason = PriorityReason.UserExplicit,
            ExecutionStrategy = ExecutionStrategy.Delayed,
            EstimatedDuration = TimeSpan.FromMinutes(3),
            MaxWaitTime = TimeSpan.FromMinutes(10),
            RequiredCapability = "CodeGeneration",
            TargetAgentId = "agent-1",
            Status = MafTaskStatus.Running,
            StartedAt = now,
            CompletedAt = now.AddMinutes(1)
        };
        task.Parameters["lang"] = "C#";
        task.Context["env"] = "test";

        task.TaskId.Should().Be("t-1");
        task.Priority.Should().Be(TaskPriority.High);
        task.PriorityScore.Should().Be(85);
        task.ExecutionStrategy.Should().Be(ExecutionStrategy.Delayed);
        task.TargetAgentId.Should().Be("agent-1");
        task.Status.Should().Be(MafTaskStatus.Running);
        task.StartedAt.Should().Be(now);
        task.CompletedAt.Should().Be(now.AddMinutes(1));
        task.Parameters.Should().ContainKey("lang");
        task.Context.Should().ContainKey("env");
    }

    [Fact]
    public void DecomposedTask_ResourceRequirements_DefaultCreated()
    {
        var task = new DecomposedTask();
        task.ResourceRequirements.Should().NotBeNull();
        task.ResourceRequirements.RequiredAgentSlots.Should().Be(1);
    }

    [Fact]
    public void DecomposedTask_Result_NullByDefault()
    {
        var task = new DecomposedTask();
        task.Result.Should().BeNull();

        var result = new TaskExecutionResult { TaskId = "t-1", Success = true };
        task.Result = result;
        task.Result.Should().NotBeNull();
        task.Result!.Success.Should().BeTrue();
    }

    // === ResourceRequirements ===

    [Fact]
    public void ResourceRequirements_Defaults()
    {
        var r = new ResourceRequirements();
        r.RequiredAgentSlots.Should().Be(1);
        r.MaxExecutionTime.Should().Be(TimeSpan.FromMinutes(5));
        r.RequiresExclusiveAccess.Should().BeFalse();
        r.RequiredCapabilities.Should().BeEmpty();
    }

    [Fact]
    public void ResourceRequirements_SetAll()
    {
        var r = new ResourceRequirements
        {
            RequiredAgentSlots = 3,
            MaxExecutionTime = TimeSpan.FromMinutes(10),
            RequiresExclusiveAccess = true,
            RequiredCapabilities = new List<string> { "CodeGen", "Testing" }
        };

        r.RequiredAgentSlots.Should().Be(3);
        r.MaxExecutionTime.Should().Be(TimeSpan.FromMinutes(10));
        r.RequiresExclusiveAccess.Should().BeTrue();
        r.RequiredCapabilities.Should().HaveCount(2);
    }

    // === MainTask ===

    [Fact]
    public void MainTask_SubTasks_Navigation()
    {
        var main = new MainTask { Title = "主任务", Description = "描述" };
        main.SubTasks.Should().BeEmpty();

        var sub = new SubTask { Title = "子任务" };
        main.SubTasks.Add(sub);
        main.SubTasks.Should().HaveCount(1);
    }

    [Fact]
    public void MainTask_UpdatedAt_SetAndGet()
    {
        var main = new MainTask();
        main.UpdatedAt.Should().BeNull();
        var now = DateTime.UtcNow;
        main.UpdatedAt = now;
        main.UpdatedAt.Should().Be(now);
    }

    // === DecompositionMetadata ===

    [Fact]
    public void DecompositionMetadata_SetProperties()
    {
        var meta = new DecompositionMetadata
        {
            ElapsedMs = 150,
            Strategy = "LLM"
        };

        meta.ElapsedMs.Should().Be(150);
        meta.Strategy.Should().Be("LLM");
        meta.DecomposedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // === TaskDependency CheckSatisfied with DataDependency edge case ===

    [Fact]
    public void TaskDependency_CheckSatisfied_UnknownType_ReturnsFalse()
    {
        var dep = new TaskDependency { Type = (DependencyType)999 };
        var target = new DecomposedTask { Status = MafTaskStatus.Completed };
        dep.CheckSatisfied(target).Should().BeFalse();
    }

    [Fact]
    public void TaskDependency_Condition_CanBeSet()
    {
        var dep = new TaskDependency
        {
            DependsOnTaskId = "task-a",
            Condition = "Result.Data.Confidence > 0.8"
        };
        dep.Condition.Should().NotBeNullOrEmpty();
    }

    // === RagQueryRequest ===

    [Fact]
    public void RagQueryRequest_Defaults()
    {
        var r = new RagQueryRequest();
        r.Query.Should().BeEmpty();
        r.CollectionName.Should().BeEmpty();
        r.TopK.Should().Be(5);
        r.ScoreThreshold.Should().Be(0.7f);
        r.ConversationHistory.Should().BeNull();
    }

    [Fact]
    public void RagQueryRequest_SetConversationHistory()
    {
        var r = new RagQueryRequest
        {
            Query = "检索测试",
            CollectionName = "test-kb",
            TopK = 10,
            ScoreThreshold = 0.5f,
            ConversationHistory = new List<string> { "先前对话1", "先前对话2" }
        };

        r.ConversationHistory.Should().HaveCount(2);
        r.TopK.Should().Be(10);
    }
}
