// src/tests/UnitTests/Helpers/TestDataBuilder.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Tests.Helpers;

public static class TestDataBuilder
{
    public static MainTask CreateMainTask(Action<MainTask>? configure = null)
    {
        var task = new MainTask
        {
            Title = "Test Task",
            Description = "Test Description",
            Priority = TaskPriority.Normal,
            Status = MafTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(task);
        return task;
    }

    public static SubTask CreateSubTask(Action<SubTask>? configure = null)
    {
        var subTask = new SubTask
        {
            Title = "Test SubTask",
            Description = "Test SubTask Description",
            Status = MafTaskStatus.Pending,
            ExecutionOrder = 1
        };
        configure?.Invoke(subTask);
        return subTask;
    }

    public static SchedulePlanEntity CreateSchedulePlan(Action<SchedulePlanEntity>? configure = null)
    {
        var plan = new SchedulePlanEntity
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanJson = "{}",
            Status = SchedulePlanStatus.Created,
            TotalTasks = 1,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(plan);
        return plan;
    }

    public static ExecutionPlanEntity CreateExecutionPlan(Action<ExecutionPlanEntity>? configure = null)
    {
        var plan = new ExecutionPlanEntity
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanJson = "{}",
            Status = ExecutionPlanStatus.Created,
            TotalTasks = 1,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(plan);
        return plan;
    }

    public static TaskExecutionResultEntity CreateTaskExecutionResult(Action<TaskExecutionResultEntity>? configure = null)
    {
        var result = new TaskExecutionResultEntity
        {
            TaskId = Guid.NewGuid().ToString(),
            PlanId = Guid.NewGuid().ToString(),
            Success = true,
            StartedAt = DateTime.UtcNow
        };
        configure?.Invoke(result);
        return result;
    }

    public static LlmProviderConfig CreateLlmConfig(Action<LlmProviderConfig>? configure = null)
    {
        var config = new LlmProviderConfig
        {
            ProviderName = "test-provider",
            ProviderDisplayName = "Test Provider",
            ApiBaseUrl = "https://api.test.com",
            ApiKey = "test-key-12345678",
            ModelId = "test-model",
            ModelDisplayName = "Test Model",
            IsEnabled = true,
            Priority = 1,
            SupportedScenarios = new List<LlmScenario> { LlmScenario.Chat }
        };
        configure?.Invoke(config);
        return config;
    }

    public static DecomposedTask CreateDecomposedTask(Action<DecomposedTask>? configure = null)
    {
        var task = new DecomposedTask
        {
            TaskId = Guid.NewGuid().ToString(),
            TaskName = "Test Task",
            Intent = "TestIntent",
            Description = "Test Description",
            Priority = TaskPriority.Normal,
            PriorityScore = 50,
            RequiredCapability = "test"
        };
        configure?.Invoke(task);
        return task;
    }
}
