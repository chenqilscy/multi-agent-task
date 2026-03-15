using System.Diagnostics;
using CKY.MultiAgentFramework.Core.Diagnostics;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using CKY.MultiAgentFramework.Services.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Tracing
{
    /// <summary>
    /// 分布式追踪 ActivitySource 单元测试
    /// 验证 Activity 正确创建、tag 正确设置
    /// </summary>
    public class MafActivitySourceTests : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly List<Activity> _capturedActivities = new();

        public MafActivitySourceTests()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source =>
                    source.Name == MafActivitySource.AgentSourceName ||
                    source.Name == MafActivitySource.TaskSourceName ||
                    source.Name == MafActivitySource.LlmSourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => _capturedActivities.Add(activity)
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        [Fact]
        public void MafActivitySource_ShouldDefineThreeSources()
        {
            MafActivitySource.AllSourceNames.Should().HaveCount(3);
            MafActivitySource.AllSourceNames.Should().Contain(MafActivitySource.AgentSourceName);
            MafActivitySource.AllSourceNames.Should().Contain(MafActivitySource.TaskSourceName);
            MafActivitySource.AllSourceNames.Should().Contain(MafActivitySource.LlmSourceName);
        }

        [Fact]
        public void AgentSource_ShouldCreateActivity()
        {
            using var activity = MafActivitySource.Agent.StartActivity("test.agent");

            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be("test.agent");
            activity.Source.Name.Should().Be(MafActivitySource.AgentSourceName);
        }

        [Fact]
        public void TaskSource_ShouldCreateActivity()
        {
            using var activity = MafActivitySource.Task.StartActivity("test.task");

            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be("test.task");
            activity.Source.Name.Should().Be(MafActivitySource.TaskSourceName);
        }

        [Fact]
        public void LlmSource_ShouldCreateActivity()
        {
            using var activity = MafActivitySource.Llm.StartActivity("test.llm");

            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be("test.llm");
            activity.Source.Name.Should().Be(MafActivitySource.LlmSourceName);
        }

        [Fact]
        public void Activity_ShouldSupportTags()
        {
            using var activity = MafActivitySource.Agent.StartActivity("test.tags");

            activity?.SetTag("agent.id", "test-agent-1");
            activity?.SetTag("agent.success", true);
            activity?.SetTag("agent.duration_ms", 500L);

            activity.Should().NotBeNull();
            activity!.GetTagItem("agent.id").Should().Be("test-agent-1");
            activity.GetTagItem("agent.success").Should().Be(true);
            activity.GetTagItem("agent.duration_ms").Should().Be(500L);
        }

        [Fact]
        public void Activity_ShouldSupportStatus()
        {
            using var activity = MafActivitySource.Agent.StartActivity("test.status");

            activity?.SetStatus(ActivityStatusCode.Error, "test error");

            activity.Should().NotBeNull();
            activity!.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("test error");
        }

        [Fact]
        public void Activity_ShouldSupportNestedSpans()
        {
            using var parent = MafActivitySource.Agent.StartActivity("parent.agent");
            using var child = MafActivitySource.Llm.StartActivity("child.llm");

            parent.Should().NotBeNull();
            child.Should().NotBeNull();

            // 子 Activity 应自动关联父 Activity
            child!.ParentId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task TaskScheduler_ScheduleAsync_ShouldCreateActivity()
        {
            var calculator = new MafPriorityCalculator(NullLogger<MafPriorityCalculator>.Instance);
            var scheduler = new MafTaskScheduler(calculator, maxConcurrentTasks: 3);

            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "t1", TaskName = "Test Task", Priority = TaskPriority.Normal }
            };

            _capturedActivities.Clear();
            await scheduler.ScheduleAsync(tasks);

            _capturedActivities.Should().Contain(a => a.OperationName == "task.schedule");
            var scheduleActivity = _capturedActivities.First(a => a.OperationName == "task.schedule");
            scheduleActivity.GetTagItem("task.count").Should().Be(1);
        }

        [Fact]
        public async Task TaskScheduler_ExecuteTaskAsync_ShouldCreateActivity()
        {
            var calculator = new MafPriorityCalculator(NullLogger<MafPriorityCalculator>.Instance);
            var scheduler = new MafTaskScheduler(calculator, maxConcurrentTasks: 3);

            var task = new DecomposedTask
            {
                TaskId = "t1",
                TaskName = "Exec Test",
                Priority = TaskPriority.High,
                PriorityScore = 80
            };

            _capturedActivities.Clear();
            var result = await scheduler.ExecuteTaskAsync(task, (t, ct) =>
                System.Threading.Tasks.Task.FromResult(new TaskExecutionResult
                {
                    TaskId = t.TaskId,
                    Success = true,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                }));

            _capturedActivities.Should().Contain(a => a.OperationName == "task.execute");
            var execActivity = _capturedActivities.First(a => a.OperationName == "task.execute");
            execActivity.GetTagItem("task.id").Should().Be("t1");
            execActivity.GetTagItem("task.name").Should().Be("Exec Test");
            execActivity.GetTagItem("task.success").Should().Be(true);
        }

        [Fact]
        public async Task TaskOrchestrator_CreatePlanAsync_ShouldCreateActivity()
        {
            var orchestrator = new MafTaskOrchestrator(NullLogger<MafTaskOrchestrator>.Instance);

            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "t1", TaskName = "Plan Task 1" },
                new() { TaskId = "t2", TaskName = "Plan Task 2" }
            };

            _capturedActivities.Clear();
            await orchestrator.CreatePlanAsync(tasks);

            _capturedActivities.Should().Contain(a => a.OperationName == "task.create_plan");
            var planActivity = _capturedActivities.First(a => a.OperationName == "task.create_plan");
            planActivity.GetTagItem("task.count").Should().Be(2);
        }

        [Fact]
        public async Task TaskOrchestrator_ExecutePlanAsync_ShouldCreateActivity()
        {
            var orchestrator = new MafTaskOrchestrator(NullLogger<MafTaskOrchestrator>.Instance);

            var tasks = new List<DecomposedTask>
            {
                new() { TaskId = "t1", TaskName = "Plan Exec Task" }
            };

            var plan = await orchestrator.CreatePlanAsync(tasks);

            _capturedActivities.Clear();
            await orchestrator.ExecutePlanAsync(plan);

            _capturedActivities.Should().Contain(a => a.OperationName == "task.execute_plan");
            var execActivity = _capturedActivities.First(a => a.OperationName == "task.execute_plan");
            execActivity.GetTagItem("plan.id").Should().NotBeNull();
        }
    }
}
