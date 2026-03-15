using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Session;
using CKY.MultiAgentFramework.Services.Dialog;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.E2E
{
    /// <summary>
    /// 测试用的会话存储
    /// </summary>
    public class TestSessionStorage : IMafSessionStorage
    {
        private readonly Dictionary<string, MafAgentSession> _sessions = new();

        public Task<IAgentSession> LoadSessionAsync(string conversationId, CancellationToken ct = default)
        {
            if (_sessions.TryGetValue(conversationId, out var session))
            {
                return Task.FromResult(session as IAgentSession);
            }
            return Task.FromResult(new MafAgentSession { Id = conversationId } as IAgentSession);
        }

        public Task SaveSessionAsync(IAgentSession session, CancellationToken ct = default)
        {
            if (session is MafAgentSession mafSession)
            {
                _sessions[mafSession.Id] = mafSession;
            }
            return Task.CompletedTask;
        }

        public Task DeleteSessionAsync(string conversationId, CancellationToken ct = default)
        {
            _sessions.Remove(conversationId);
            return Task.CompletedTask;
        }

        public Task AddMessageAsync(string conversationId, IMessage message, CancellationToken ct = default)
        {
            if (!_sessions.ContainsKey(conversationId))
            {
                _sessions[conversationId] = new MafAgentSession { Id = conversationId };
            }
            return Task.CompletedTask;
        }

        public Task<IAgentSession?> GetSessionAsync(string conversationId, CancellationToken ct = default)
            => Task.FromResult<IAgentSession?>(null);

        public Task UpdateSessionAsync(IAgentSession session, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IEnumerable<IAgentSession>> GetAllSessionsAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<IAgentSession>>(_sessions.Values.Cast<IAgentSession>());

        public Task<bool> ExistsAsync(string conversationId, CancellationToken ct = default)
            => Task.FromResult(_sessions.ContainsKey(conversationId));
    }

    /// <summary>
    /// 端到端测试：智能家居场景
    /// E2E tests for smart home scenarios
    /// </summary>
    public class SmartHomeScenariosE2E
    {
        private readonly TestSessionStorage _sessionStorage;
        private readonly SlotManager _slotManager;
        private readonly MafCoreferenceResolver _coreferenceResolver;
        private readonly IntentDriftDetector _driftDetector;
        private readonly DialogStateManager _stateManager;

        public SmartHomeScenariosE2E()
        {
            _sessionStorage = new TestSessionStorage();
            _slotManager = new SlotManager(NullLogger<SlotManager>.Instance);
            _coreferenceResolver = new MafCoreferenceResolver(_sessionStorage, NullLogger<MafCoreferenceResolver>.Instance);
            _driftDetector = new IntentDriftDetector(NullLogger<IntentDriftDetector>.Instance);
            _stateManager = new DialogStateManager(NullLogger<DialogStateManager>.Instance);
        }

        /// <summary>
        /// Scenario 1: 简单查询 - "查询北京今天天气"
        /// Expected: 直接完成，无需槽位填充或澄清
        /// </summary>
        [Fact]
        public async Task Scenario1_SimpleQuery_ShouldCompleteDirectly()
        {
            // Arrange
            var context = new DialogContext { SessionId = "session1", UserId = "user1" };
            var intent = "query_weather";
            var userInput = "查询北京今天天气";

            // Act: 检测槽位
            var slots = await _slotManager.DetectSlotsAsync(intent, userInput);

            // Assert: 所有必需槽位应该已填充
            var missingSlots = _slotManager.GetMissingSlots(intent, slots);
            missingSlots.Should().BeEmpty("all required slots should be detected from input");

            slots["location"].Value.Should().Be("北京");
            slots["date"].Value.Should().Be("今天");
        }

        /// <summary>
        /// Scenario 2: 设备控制 + 澄清 + 指代消解
        /// Turn 1: "打开客厅空调" -> 检测槽位
        /// Turn 2: "再低一点" -> 指代消解 -> 槽位填充
        /// </summary>
        [Fact]
        public async Task Scenario2_DeviceControlWithClarificationAndCoreference_ShouldComplete()
        {
            // Arrange
            var conversationId = "conv2";
            var context = new DialogContext { SessionId = conversationId, UserId = "user1" };
            var intent = "control_device";

            // Turn 1: "打开客厅空调"
            var turn1 = "打开客厅空调";
            var session = new MafAgentSession { Id = conversationId };
            await _sessionStorage.SaveSessionAsync(session);

            var slots1 = await _slotManager.DetectSlotsAsync(intent, turn1);
            await _slotManager.FillSlotAsync(context, intent, "device", "空调");
            await _slotManager.FillSlotAsync(context, intent, "location", "客厅");
            await _slotManager.FillSlotAsync(context, intent, "action", "打开");

            // 验证 Turn 1 槽位
            var filled1 = await _slotManager.GetFilledSlotsAsync(context, intent);
            filled1["device"].Value.Should().Be("空调");
            filled1["location"].Value.Should().Be("客厅");
            filled1["action"].Value.Should().Be("打开");

            // Turn 2: "再低一点" (指代空调温度)
            var turn2 = "再低一点";

            // 指代消解
            var resolved = await _coreferenceResolver.ResolveAsync(turn2, conversationId);
            resolved.Should().Contain("空调", "pronoun should be resolved");

            // 检测新槽位
            var slots2 = await _slotManager.DetectSlotsAsync(intent, resolved);
            await _slotManager.FillSlotAsync(context, intent, "adjustment", "低");

            // 验证最终槽位
            var filled2 = await _slotManager.GetFilledSlotsAsync(context, intent);
            filled2["device"].Value.Should().Be("空调");
            filled2["adjustment"].Value.Should().Be("低");
        }

        /// <summary>
        /// Scenario 3: 复杂多轮对话 + 意图漂移
        /// Turn 1: "打开客厅的灯" (control_device)
        /// Turn 2: "把它调亮一点" (继续控制设备)
        /// Turn 3: "对了，播放周杰伦的歌" (意图漂移到 PlayMusic)
        /// Turn 4: "回到刚才" (回退到设备控制)
        /// </summary>
        [Fact]
        public async Task Scenario3_ComplexMultiTurnWithDriftAndRollback_ShouldHandleAll()
        {
            // Arrange
            var conversationId = "conv3";
            var context = new DialogContext { SessionId = conversationId, UserId = "user1" };

            // Turn 1: "打开客厅的灯"
            var turn1 = "打开客厅的灯";
            var session = new MafAgentSession { Id = conversationId };
            await _sessionStorage.SaveSessionAsync(session);

            var state1 = new DialogState
            {
                CurrentIntent = "control_device",
                SlotValues = new Dictionary<string, object>
                {
                    ["device"] = "灯",
                    ["location"] = "客厅",
                    ["action"] = "打开"
                },
                UserInputs = new List<string> { turn1 }
            };
            await _stateManager.PushStateAsync(state1);

            // 验证状态
            var current1 = await _stateManager.GetCurrentStateAsync();
            current1!.CurrentIntent.Should().Be("control_device");
            _stateManager.StackDepth.Should().Be(1);

            // Turn 2: "把它调亮一点" (继续设备控制)
            var turn2 = "把它调亮一点";

            var resolved2 = await _coreferenceResolver.ResolveAsync(turn2, conversationId);
            resolved2.Should().Contain("灯");

            // 检测意图漂移 - 不应该漂移
            var drift2 = await _driftDetector.DetectDriftAsync(turn2, "control_device", context);
            drift2.HasDrifted.Should().BeFalse("still talking about the same device");

            // Turn 3: "对了，播放周杰伦的歌" (意图漂移)
            var turn3 = "对了，播放周杰伦的歌";

            // 检测意图漂移
            var drift3 = await _driftDetector.DetectDriftAsync(turn3, "control_device", context);
            drift3.HasDrifted.Should().BeTrue("user switched to music");
            drift3.SuggestedAction.Should().Be(DriftAction.NewTopic);

            // 保存当前状态并推入新状态
            var shouldSave = await _stateManager.HandleTopicSwitchAsync("PlayMusic");
            shouldSave.Should().BeTrue();

            var state3 = new DialogState
            {
                CurrentIntent = "PlayMusic",
                SlotValues = new Dictionary<string, object>
                {
                    ["artist"] = "周杰伦"
                },
                UserInputs = new List<string> { turn3 }
            };
            await _stateManager.PushStateAsync(state3);

            _stateManager.StackDepth.Should().Be(2, "should have two states on stack");

            // Turn 4: "回到刚才" (回退)
            var turn4 = "回到刚才";

            // 回退到设备控制
            var rolledBack = await _stateManager.RollbackAsync();
            rolledBack.Should().BeTrue();

            var current4 = await _stateManager.GetCurrentStateAsync();
            current4!.CurrentIntent.Should().Be("control_device");
            current4.SlotValues["device"].Should().Be("灯");
            _stateManager.StackDepth.Should().Be(1);
        }

        /// <summary>
        /// Scenario 4: 槽位缺失 -> 澄清 -> 补充 -> 完成
        /// "打开空调" -> "哪个房间？" -> "客厅" -> 完成
        /// </summary>
        [Fact]
        public async Task Scenario4_SlotClarificationFlow_ShouldComplete()
        {
            // Arrange
            var context = new DialogContext { SessionId = "session4", UserId = "user1" };
            var intent = "control_device";

            // Turn 1: 不完整的输入
            var input1 = "打开空调";
            var slots1 = await _slotManager.DetectSlotsAsync(intent, input1);
            await _slotManager.FillSlotAsync(context, intent, "device", "空调");
            await _slotManager.FillSlotAsync(context, intent, "action", "打开");

            var missing1 = _slotManager.GetMissingSlots(intent, slots1);
            missing1.Should().Contain("location", "location slot should be missing");

            // 生成澄清问题
            var questions = _slotManager.GenerateClarificationQuestions(intent, missing1);
            questions.Should().HaveCountGreaterThan(0);
            questions[0].Question.Should().Contain("位置");

            // Turn 2: 用户补充信息
            var input2 = "客厅的";
            var slots2 = await _slotManager.DetectSlotsAsync(intent, input2);
            await _slotManager.FillSlotAsync(context, intent, "location", "客厅");

            // 验证所有槽位已填充
            var filledSlots = await _slotManager.GetFilledSlotsAsync(context, intent);
            filledSlots["device"].Value.Should().Be("空调");
            filledSlots["location"].Value.Should().Be("客厅");
            filledSlots["action"].Value.Should().Be("打开");

            // 再次检查缺失槽位
            var missing2 = _slotManager.GetMissingSlots(intent, filledSlots);
            missing2.Should().BeEmpty("all required slots should now be filled");
        }

        /// <summary>
        /// Scenario 5: 多设备控制 + 历史槽位推断
        /// Turn 1: "打开客厅的灯"
        /// Turn 2: "再打开卧室的" (推断: 还是打开灯)
        /// </summary>
        [Fact]
        public async Task Scenario5_MultiDeviceWithHistoricalInference_ShouldInferDevice()
        {
            // Arrange
            var conversationId = "conv5";
            var context = new DialogContext { SessionId = conversationId, UserId = "user1" };
            var intent = "control_device";

            // Turn 1: 打开客厅灯
            var turn1 = "打开客厅的灯";
            var slots1 = await _slotManager.DetectSlotsAsync(intent, turn1);
            await _slotManager.FillSlotAsync(context, intent, "device", "灯");
            await _slotManager.FillSlotAsync(context, intent, "location", "客厅");
            await _slotManager.FillSlotAsync(context, intent, "action", "打开");

            var session1 = new MafAgentSession { Id = conversationId };
            await _sessionStorage.SaveSessionAsync(session1);

            // 更新历史槽位
            context.HistoricalSlots[$"{intent}.device"] = "灯";
            context.HistoricalSlots[$"{intent}.action"] = "打开";
            context.TurnCount = 1;

            // Turn 2: "再打开卧室的" (省略设备)
            var turn2 = "再打开卧室的";
            var slots2 = await _slotManager.DetectSlotsAsync(intent, turn2);

            // 从历史推断设备类型
            if (!slots2.ContainsKey("device") && context.HistoricalSlots.ContainsKey($"{intent}.device"))
            {
                var historicalDevice = context.HistoricalSlots[$"{intent}.device"].ToString();
                await _slotManager.FillSlotAsync(context, intent, "device", historicalDevice!);
            }

            await _slotManager.FillSlotAsync(context, intent, "location", "卧室");
            await _slotManager.FillSlotAsync(context, intent, "action", "打开");

            // 验证推断正确
            var filledSlots = await _slotManager.GetFilledSlotsAsync(context, intent);
            filledSlots["device"].Value.Should().Be("灯", "should infer device from history");
            filledSlots["location"].Value.Should().Be("卧室");
            filledSlots["action"].Value.Should().Be("打开");
        }
    }
}
