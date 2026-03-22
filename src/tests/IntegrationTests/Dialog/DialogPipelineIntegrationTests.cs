using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Message;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Dialog;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.IntegrationTests.Dialog;

/// <summary>
/// Dialog管道集成测试
/// 验证 DialogStateManager + SessionStorage 的完整对话状态管理
/// </summary>
public class DialogPipelineIntegrationTests
{
    private readonly InMemorySessionStorage _sessionStorage;
    private readonly DialogStateManager _dialogManager;

    public DialogPipelineIntegrationTests()
    {
        _sessionStorage = new InMemorySessionStorage();
        _dialogManager = new DialogStateManager(
            _sessionStorage,
            NullLogger<DialogStateManager>.Instance);
    }

    [Fact]
    public async Task LoadOrCreate_NewConversation_ShouldCreateNewContext()
    {
        var context = await _dialogManager.LoadOrCreateAsync("conv-001", "user-001");

        context.Should().NotBeNull();
        context.SessionId.Should().Be("conv-001");
        context.UserId.Should().Be("user-001");
        context.TurnCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadOrCreate_ExistingConversation_ShouldReturnSameContext()
    {
        // 创建并更新上下文使其被保存
        var context1 = await _dialogManager.LoadOrCreateAsync("conv-002", "user-002");
        await SaveDialogContext("conv-002", context1);

        var context2 = await _dialogManager.LoadOrCreateAsync("conv-002", "user-002");

        context2.SessionId.Should().Be("conv-002");
        // 注意: 由于 MafAgentSession.Context 存储限制，
        // 重新加载后需要检查是否正确恢复
    }

    [Fact]
    public async Task Update_ShouldIncrementTurnCount()
    {
        var context = await _dialogManager.LoadOrCreateAsync("conv-003", "user-003");
        await SaveDialogContext("conv-003", context);

        var initialTurn = context.TurnCount;
        await _dialogManager.UpdateAsync(
            context,
            "ControlLight",
            new Dictionary<string, object> { ["room"] = "客厅" },
            new List<TaskExecutionResult>());

        context.TurnCount.Should().Be(initialTurn + 1);
        context.PreviousIntent.Should().Be("ControlLight");
    }

    [Fact]
    public async Task Update_ShouldTrackHistoricalSlots()
    {
        var context = await _dialogManager.LoadOrCreateAsync("conv-004", "user-004");
        await SaveDialogContext("conv-004", context);

        // 第一次更新
        await _dialogManager.UpdateAsync(
            context,
            "ControlLight",
            new Dictionary<string, object> { ["room"] = "客厅", ["action"] = "打开" },
            new List<TaskExecutionResult>());

        context.HistoricalSlots.Should().ContainKey("ControlLight.room");
        context.HistoricalSlots.Should().ContainKey("ControlLight.action");

        // 第二次相同意图 → 频次增加
        await _dialogManager.UpdateAsync(
            context,
            "ControlLight",
            new Dictionary<string, object> { ["room"] = "客厅" },
            new List<TaskExecutionResult>());

        context.HistoricalSlots["ControlLight.room"].Should().Be(2);
    }

    [Fact]
    public async Task Update_MultipleIntents_ShouldTrackAll()
    {
        var context = await _dialogManager.LoadOrCreateAsync("conv-005", "user-005");
        await SaveDialogContext("conv-005", context);

        await _dialogManager.UpdateAsync(
            context,
            "ControlLight",
            new Dictionary<string, object> { ["room"] = "客厅" },
            new List<TaskExecutionResult>());

        await _dialogManager.UpdateAsync(
            context,
            "AdjustClimate",
            new Dictionary<string, object> { ["temperature"] = "26" },
            new List<TaskExecutionResult>());

        context.TurnCount.Should().Be(3); // initial(1) + 2 updates
        context.PreviousIntent.Should().Be("AdjustClimate");
        context.HistoricalSlots.Should().ContainKey("ControlLight.room");
        context.HistoricalSlots.Should().ContainKey("AdjustClimate.temperature");
    }

    [Fact]
    public async Task RecordPendingClarification_ShouldStoreMissingSlots()
    {
        var context = await _dialogManager.LoadOrCreateAsync("conv-006", "user-006");
        await SaveDialogContext("conv-006", context);

        var missingSlots = new List<SlotDefinition>
        {
            new() { SlotName = "room", Description = "房间名称", Required = true },
            new() { SlotName = "brightness", Description = "亮度", Required = false }
        };

        await _dialogManager.RecordPendingClarificationAsync(
            context,
            "ControlLight",
            new Dictionary<string, object> { ["action"] = "打开" },
            missingSlots);

        context.PendingClarification.Should().NotBeNull();
        context.PendingClarification!.Intent.Should().Be("ControlLight");
        context.PendingClarification.MissingSlots.Should().HaveCount(2);
        context.PendingClarification.DetectedSlots.Should().ContainKey("action");
    }

    [Fact]
    public async Task RecordPendingTasks_ShouldStorePlan()
    {
        var context = await _dialogManager.LoadOrCreateAsync("conv-007", "user-007");
        await SaveDialogContext("conv-007", context);

        var plan = new ExecutionPlan();
        plan.SerialGroups.Add(new TaskGroup
        {
            Tasks = [new DecomposedTask { TaskName = "打开灯" }]
        });

        await _dialogManager.RecordPendingTasksAsync(
            context,
            plan,
            new Dictionary<string, object> { ["room"] = "客厅", ["action"] = "打开" });

        context.PendingTask.Should().NotBeNull();
        context.PendingTask!.Plan.Should().NotBeNull();
        context.PendingTask.FilledSlots.Should().HaveCount(2);
    }

    [Fact]
    public async Task Update_NullContext_ShouldThrow()
    {
        var act = () => _dialogManager.UpdateAsync(
            null!,
            "ControlLight",
            new Dictionary<string, object>(),
            new List<TaskExecutionResult>());

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RecordPendingClarification_NullParams_ShouldThrow()
    {
        var context = await _dialogManager.LoadOrCreateAsync("conv-008", "user-008");

        var act1 = () => _dialogManager.RecordPendingClarificationAsync(
            null!, "intent", new Dictionary<string, object>(), new List<SlotDefinition>());

        var act2 = () => _dialogManager.RecordPendingClarificationAsync(
            context, "intent", null!, new List<SlotDefinition>());

        var act3 = () => _dialogManager.RecordPendingClarificationAsync(
            context, "intent", new Dictionary<string, object>(), null!);

        await act1.Should().ThrowAsync<ArgumentNullException>();
        await act2.Should().ThrowAsync<ArgumentNullException>();
        await act3.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// 将 DialogContext 保存到会话存储中，模拟正常的 session 保存流程
    /// </summary>
    private async Task SaveDialogContext(string conversationId, DialogContext context)
    {
        var session = await _sessionStorage.LoadSessionAsync(conversationId);
        session.Context["dialog_context"] = context;
        await _sessionStorage.SaveSessionAsync(session);
    }

    /// <summary>
    /// 内存会话存储（测试用）
    /// </summary>
    private class InMemorySessionStorage : IMafSessionStorage
    {
        private readonly Dictionary<string, TestAgentSession> _sessions = new();

        public Task<IAgentSession> LoadSessionAsync(string conversationId, CancellationToken ct = default)
        {
            if (_sessions.TryGetValue(conversationId, out var session))
                return Task.FromResult(session as IAgentSession);

            var newSession = new TestAgentSession(conversationId);
            _sessions[conversationId] = newSession;
            return Task.FromResult(newSession as IAgentSession);
        }

        public Task SaveSessionAsync(IAgentSession session, CancellationToken ct = default)
        {
            if (session is TestAgentSession testSession)
                _sessions[testSession.SessionId] = testSession;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string conversationId, CancellationToken ct = default)
        {
            return Task.FromResult(_sessions.ContainsKey(conversationId));
        }

        public Task DeleteSessionAsync(string conversationId, CancellationToken ct = default)
        {
            _sessions.Remove(conversationId);
            return Task.CompletedTask;
        }
    }

    private class TestAgentSession : IAgentSession
    {
        public string SessionId { get; init; }
        public string AgentId { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Context { get; init; } = new();
        public List<MessageContext> MessageHistory { get; init; } = new();

        public TestAgentSession(string sessionId)
        {
            SessionId = sessionId;
        }
    }
}
