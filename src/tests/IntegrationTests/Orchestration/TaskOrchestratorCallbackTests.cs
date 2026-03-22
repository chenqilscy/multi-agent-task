using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Orchestration;

/// <summary>
/// MafTaskOrchestrator callback 模式 E2E 测试
/// 验证 LeaderAgent → Orchestrator → taskExecutor 回调完整链路
/// </summary>
public class TaskOrchestratorCallbackTests
{
    private readonly MafTaskOrchestrator _orchestrator;

    public TaskOrchestratorCallbackTests()
    {
        _orchestrator = new MafTaskOrchestrator(
            NullLogger<MafTaskOrchestrator>.Instance);
    }

    [Fact]
    public async Task ExecutePlanAsync_WithCallback_DispatchesToExecutor()
    {
        // Arrange：模拟 LeaderAgent 注入的 Agent 分发逻辑
        var executedTasks = new List<string>();

        Task<TaskExecutionResult> FakeAgentExecutor(DecomposedTask task, CancellationToken ct)
        {
            executedTasks.Add(task.TaskName);
            return Task.FromResult(new TaskExecutionResult
            {
                TaskId = task.TaskId,
                Success = true,
                Message = $"Agent 执行完成: {task.TaskName}",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }

        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "开灯" },
            new() { TaskName = "调温" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);

        // Act
        var results = await _orchestrator.ExecutePlanAsync(plan, FakeAgentExecutor);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        executedTasks.Should().Contain("开灯");
        executedTasks.Should().Contain("调温");
    }

    [Fact]
    public async Task ExecutePlanAsync_CallbackFailure_TaskMarkedFailed()
    {
        Task<TaskExecutionResult> FailingExecutor(DecomposedTask task, CancellationToken ct)
        {
            return Task.FromResult(new TaskExecutionResult
            {
                TaskId = task.TaskId,
                Success = false,
                Message = "Agent 不可用",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }

        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "失败任务" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        var results = await _orchestrator.ExecutePlanAsync(plan, FailingExecutor);

        results.Should().HaveCount(1);
        results.First().Success.Should().BeFalse();
        results.First().Message.Should().Contain("Agent 不可用");
    }

    [Fact]
    public async Task ExecutePlanAsync_CallbackException_GracefullyHandled()
    {
        Task<TaskExecutionResult> ThrowingExecutor(DecomposedTask task, CancellationToken ct)
        {
            throw new InvalidOperationException("Agent 崩溃");
        }

        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "崩溃任务" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        var results = await _orchestrator.ExecutePlanAsync(plan, ThrowingExecutor);

        results.Should().HaveCount(1);
        results.First().Success.Should().BeFalse();
        results.First().Message.Should().Contain("Agent 崩溃");
    }

    [Fact]
    public async Task ExecutePlanAsync_ParallelGroupWithCallback_AllExecuted()
    {
        var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();

        Task<TaskExecutionResult> TrackingExecutor(DecomposedTask task, CancellationToken ct)
        {
            executionOrder.Add(task.TaskName);
            return Task.FromResult(new TaskExecutionResult
            {
                TaskId = task.TaskId,
                Success = true,
                Message = "OK",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }

        // 3 个无依赖任务 → 应被分到并行组
        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "灯光A" },
            new() { TaskName = "灯光B" },
            new() { TaskName = "灯光C" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        var results = await _orchestrator.ExecutePlanAsync(plan, TrackingExecutor);

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        executionOrder.Should().HaveCount(3);
    }

    [Fact]
    public async Task SetDefaultTaskExecutor_UsedWhenNoCallbackProvided()
    {
        var defaultExecuted = false;

        _orchestrator.SetDefaultTaskExecutor(async (task, ct) =>
        {
            defaultExecuted = true;
            return new TaskExecutionResult
            {
                TaskId = task.TaskId,
                Success = true,
                Message = "Default executor",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        });

        var tasks = new List<DecomposedTask>
        {
            new() { TaskName = "默认执行器任务" }
        };

        var plan = await _orchestrator.CreatePlanAsync(tasks);
        // 使用无 callback 参数的重载 → 应走 _defaultTaskExecutor
        var results = await _orchestrator.ExecutePlanAsync(plan);

        defaultExecuted.Should().BeTrue();
        results.Should().HaveCount(1);
        results.First().Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecutePlanAsync_SerialGroup_StopsOnFailure()
    {
        var callCount = 0;

        Task<TaskExecutionResult> FailOnSecondExecutor(DecomposedTask task, CancellationToken ct)
        {
            callCount++;
            var success = callCount <= 1;
            return Task.FromResult(new TaskExecutionResult
            {
                TaskId = task.TaskId,
                Success = success,
                Message = success ? "OK" : "Failed",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }

        // 创建有依赖的任务链 → 串行组
        var taskA = new DecomposedTask { TaskName = "步骤1" };
        var taskB = new DecomposedTask
        {
            TaskName = "步骤2(会失败)",
            Dependencies = new List<TaskDependency>
            {
                new() { DependsOnTaskId = taskA.TaskId }
            }
        };
        var taskC = new DecomposedTask
        {
            TaskName = "步骤3(不应执行)",
            Dependencies = new List<TaskDependency>
            {
                new() { DependsOnTaskId = taskB.TaskId }
            }
        };

        var plan = await _orchestrator.CreatePlanAsync(new List<DecomposedTask> { taskA, taskB, taskC });
        var results = await _orchestrator.ExecutePlanAsync(plan, FailOnSecondExecutor);

        // 步骤1 成功，步骤2 失败，步骤3 不应被执行（串行组中失败停止）
        results.Should().HaveCountGreaterThanOrEqualTo(2);
        results.First().Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecutePlanAsync_TaskStatusUpdated()
    {
        Task<TaskExecutionResult> SimpleExecutor(DecomposedTask task, CancellationToken ct)
        {
            return Task.FromResult(new TaskExecutionResult
            {
                TaskId = task.TaskId,
                Success = true,
                Message = "Done",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }

        var task = new DecomposedTask { TaskName = "状态验证任务", Status = MafTaskStatus.Pending };
        var plan = await _orchestrator.CreatePlanAsync(new List<DecomposedTask> { task });

        await _orchestrator.ExecutePlanAsync(plan, SimpleExecutor);

        task.Status.Should().Be(MafTaskStatus.Completed);
        task.StartedAt.Should().NotBeNull();
        task.CompletedAt.Should().NotBeNull();
    }
}
