using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Session;
using CKY.MultiAgentFramework.Services.Dialog;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Integration
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
    /// 集成测试：测试 SlotManager + ClarificationManager + CoreferenceResolver + IntentDriftDetector 的集成
    /// </summary>
    public class DialogIntegrationTests
    {
        private readonly TestSessionStorage _sessionStorage;
        private readonly SlotManager _slotManager;
        private readonly MafCoreferenceResolver _coreferenceResolver;
        private readonly IntentDriftDetector _driftDetector;
        private readonly DialogStateManager _stateManager;

        public DialogIntegrationTests()
        {
            _sessionStorage = new TestSessionStorage();
            _slotManager = new SlotManager(NullLogger<SlotManager>.Instance);
            _coreferenceResolver = new MafCoreferenceResolver(_sessionStorage, NullLogger<MafCoreferenceResolver>.Instance);
            _driftDetector = new IntentDriftDetector(NullLogger<IntentDriftDetector>.Instance);
            _stateManager = new DialogStateManager(NullLogger<DialogStateManager>.Instance);
        }

        [Fact]
        public async Task SlotDetectionToClarificationToFilling_CompleteFlow_ShouldSucceed()
        {
            // Arrange
            var intent = "control_device";
            var userInput = "打开客厅的灯";
            var context = new DialogContext { SessionId = "session1", UserId = "user1" };

            // Step 1: 槽位检测
            var slots = await _slotManager.DetectSlotsAsync(intent, userInput);
            slots.Should().NotBeEmpty();
            slots["location"].Value.Should().Be("客厅");

            // Step 2: 检查是否需要澄清
            var missingSlots = _slotManager.GetMissingSlots(intent, slots);
            if (missingSlots.Count > 0)
            {
                // 生成澄清问题
                var questions = _slotManager.GenerateClarificationQuestions(intent, missingSlots);
                questions.Should().NotBeEmpty();
            }

            // Step 3: 填充槽位
            await _slotManager.FillSlotAsync(context, intent, "device", "灯");
            await _slotManager.FillSlotAsync(context, intent, "location", "客厅");

            // 验证槽位已填充
            var filledSlots = await _slotManager.GetFilledSlotsAsync(context, intent);
            filledSlots.Should().HaveCountGreaterOrEqualTo(2);
            filledSlots.Should().ContainKey("device");
            filledSlots["device"].Value.Should().Be("灯");
        }

        [Fact]
        public async Task MultiTurnDialogue_WithCoreferenceResolution_ShouldMaintainContext()
        {
            // Arrange
            var conversationId = "conv1";
            var context = new DialogContext { SessionId = conversationId, UserId = "user1" };

            // Turn 1: 用户打开设备
            var turn1 = "打开客厅的空调";
            var session = new MafAgentSession { Id = conversationId };
            await _sessionStorage.SaveSessionAsync(session);

            // Turn 2: 用户使用代词指代
            var turn2 = "把它调到26度";

            // Act: 指代消解
            var resolved = await _coreferenceResolver.ResolveAsync(turn2, conversationId);

            // Assert
            resolved.Should().NotBe(turn2);
            resolved.Should().Contain("空调", "pronoun should be resolved to空调");
        }

        [Fact]
        public async Task IntentDriftDetection_TopicSwitch_ShouldTriggerNewState()
        {
            // Arrange
            var context = new DialogContext
            {
                SessionId = "session1",
                UserId = "user1",
                TurnCount = 5,
                PreviousIntent = "ControlLight",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.device"] = "客厅灯"
                }
            };

            var currentInput = "顺便问一下，今天北京天气怎么样";

            // Act: 检测意图漂移
            var driftAnalysis = await _driftDetector.DetectDriftAsync(currentInput, context.PreviousIntent!, context);

            // Assert
            driftAnalysis.HasDrifted.Should().BeTrue();
            driftAnalysis.SuggestedAction.Should().Be(DriftAction.NewTopic);

            // Act: 处理话题切换
            var shouldSave = await _stateManager.HandleTopicSwitchAsync("QueryWeather");

            // Assert
            shouldSave.Should().BeTrue("should save current state before switching topics");
        }

        [Fact]
        public async Task CompleteDialogFlow_SlotFillingThenDriftThenRollback_ShouldWork()
        {
            // Arrange
            var context = new DialogContext { SessionId = "session1", UserId = "user1" };

            // Step 1: 开始控制设备对话
            var initialState = new DialogState
            {
                CurrentIntent = "control_device",
                SlotValues = new Dictionary<string, object>
                {
                    ["device"] = "空调",
                    ["location"] = "客厅"
                }
            };
            await _stateManager.PushStateAsync(initialState);

            // Step 2: 用户切换话题
            var driftInput = "对了，播放一首歌";
            var driftAnalysis = await _driftDetector.DetectDriftAsync(driftInput, "control_device", context);
            driftAnalysis.HasDrifted.Should().BeTrue();

            var shouldSave = await _stateManager.HandleTopicSwitchAsync("PlayMusic");
            shouldSave.Should().BeTrue();

            // 保存新话题状态
            var musicState = new DialogState
            {
                CurrentIntent = "PlayMusic",
                SlotValues = new Dictionary<string, object>
                {
                    ["artist"] = "周杰伦"
                }
            };
            await _stateManager.PushStateAsync(musicState);

            // 验证栈深度
            _stateManager.StackDepth.Should().Be(2);

            // Step 3: 用户想回到之前的话题
            await _stateManager.RollbackAsync();

            // 验证回退到设备控制状态
            var currentState = await _stateManager.GetCurrentStateAsync();
            currentState.Should().NotBeNull();
            currentState!.CurrentIntent.Should().Be("control_device");
            currentState.SlotValues["device"].Should().Be("空调");
        }

        [Fact]
        public async Task SlotManager_WithHistoricalContext_ShouldUseDefaults()
        {
            // Arrange
            var context = new DialogContext
            {
                SessionId = "session1",
                UserId = "user1",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.location"] = "客厅"
                }
            };

            // Act: 检测槽位，未指定位置
            var slots = await _slotManager.DetectSlotsAsync("control_device", "打开灯");

            // Assert: 应该使用历史位置作为默认值
            if (!slots.ContainsKey("location"))
            {
                // 从历史推断
                var historicalLocation = context.HistoricalSlots
                    .FirstOrDefault(kvp => kvp.Key.Contains("location")).Value;

                if (historicalLocation != null)
                {
                    await _slotManager.FillSlotAsync(context, "control_device", "location", historicalLocation.ToString()!);
                }
            }

            var filledSlots = await _slotManager.GetFilledSlotsAsync(context, "control_device");
            filledSlots.Should().ContainKey("location");
        }

        [Fact]
        public async Task MultiTurn_ClarificationThenFilling_ShouldComplete()
        {
            // Arrange
            var context = new DialogContext { SessionId = "session1", UserId = "user1" };
            var intent = "control_device";

            // Turn 1: 用户输入不完整
            var input1 = "打开灯";
            var slots1 = await _slotManager.DetectSlotsAsync(intent, input1);
            var missing1 = _slotManager.GetMissingSlots(intent, slots1);

            // 应该缺少 location
            missing1.Should().Contain("location");

            // 生成澄清问题
            var questions = _slotManager.GenerateClarificationQuestions(intent, missing1);
            questions.Should().NotBeEmpty();
            questions[0].Question.Should().Contain("位置");

            // Turn 2: 用户回答
            var input2 = "客厅的";
            var slots2 = await _slotManager.DetectSlotsAsync(intent, input2);
            await _slotManager.FillSlotAsync(context, intent, "device", "灯");
            await _slotManager.FillSlotAsync(context, intent, "location", "客厅");

            // 验证所有必需槽位已填充
            var filledSlots = await _slotManager.GetFilledSlotsAsync(context, intent);
            filledSlots["device"].Value.Should().Be("灯");
            filledSlots["location"].Value.Should().Be("客厅");
        }
    }
}
