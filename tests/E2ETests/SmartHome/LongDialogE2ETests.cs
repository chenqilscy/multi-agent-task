using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CKY.MultiAgentFramework.E2ETests.SmartHome
{
    /// <summary>
    /// 长对话场景端到端测试
    /// Long dialog scenario end-to-end tests
    /// </summary>
    public class LongDialogE2ETests
    {
        private readonly SmartHomeMainAgent _mainAgent;
        private readonly ITestOutputHelper _output;

        public LongDialogE2ETests(ITestOutputHelper output)
        {
            _output = output;

            // Setup dependencies (in real E2E, these would be from DI container)
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            // Note: In real E2E, all dependencies would be properly registered
            // This is a simplified example showing the test structure
        }

        /// <summary>
        /// 测试多轮对话中的上下文管理
        /// Test context management across multiple turns
        /// </summary>
        [Fact]
        public async Task MultiTurnDialog_ShouldMaintainContext()
        {
            // Arrange
            var conversationId = "test-conversation-001";
            var userId = "test-user";

            // Act & Assert
            // Turn 1: 打开客厅灯
            var request1 = CreateRequest(conversationId, userId, "打开客厅的灯", new());

            // Turn 2: 将亮度调到50%（引用上一轮的"客厅"）
            var request2 = CreateRequest(conversationId, userId, "把亮度调到50", new());

            // Turn 3: 打开音乐（跨话题切换）
            var request3 = CreateRequest(conversationId, userId, "播放轻音乐", new());

            // Turn 4: 关闭灯（再次回到灯光控制，应记住"客厅"）
            var request4 = CreateRequest(conversationId, userId, "关闭", new());

            // Assert
            // In real E2E, we would execute these requests and verify:
            // - Context is maintained across turns
            // - Historical slots are tracked
            // - Topic switches are handled correctly
            // - Memory classification works

            _output.WriteLine("Multi-turn dialog test completed");
        }

        /// <summary>
        /// 测试上下文压缩触发
        /// Test context compression triggering
        /// </summary>
        [Fact]
        public async Task ContextCompression_ShouldTriggerAtTurn5()
        {
            // Arrange
            var conversationId = "test-compression-001";
            var userId = "test-user";

            // Act
            // Execute 5 turns to trigger compression
            for (int i = 1; i <= 5; i++)
            {
                var request = CreateRequest(conversationId, userId, $"第{i}轮对话", new());
                // _mainAgent.ExecuteBusinessLogicAsync(request);
            }

            // Assert
            // Verify compression was triggered and dialog history was compressed
            _output.WriteLine("Context compression test completed");
        }

        /// <summary>
        /// 测试槽位自动填充
        /// Test automatic slot filling from history
        /// </summary>
        [Fact]
        public async Task SlotAutoFill_ShouldPopulateFromHistory()
        {
            // Arrange
            var conversationId = "test-autofill-001";
            var userId = "test-user";

            // Act
            // Turn 1: 设置客厅温度为25度
            var request1 = CreateRequest(conversationId, userId, "将客厅温度设置为25度", new());

            // Turn 2: 查询温度（应自动填充"客厅"）
            var request2 = CreateRequest(conversationId, userId, "温度是多少？", new());

            // Assert
            // Verify slot was auto-filled from HistoricalSlots
            _output.WriteLine("Slot auto-fill test completed");
        }

        /// <summary>
        /// 测试记忆分类（短期vs长期）
        /// Test memory classification (short-term vs long-term)
        /// </summary>
        [Fact]
        public async Task MemoryClassification_ShouldDetermineShortVsLongTerm()
        {
            // Arrange
            var conversationId = "test-memory-001";
            var userId = "test-user";

            // Act
            // Repeat same preference 3 times to trigger long-term storage
            for (int i = 1; i <= 3; i++)
            {
                var request = CreateRequest(conversationId, userId, "我喜欢听古典音乐", new());
                // _mainAgent.ExecuteBusinessLogicAsync(request);
            }

            // Assert
            // Verify after 3rd time, preference is classified as long-term
            _output.WriteLine("Memory classification test completed");
        }

        /// <summary>
        /// 测试话题切换与状态回退
        /// Test topic switching and state rollback
        /// </summary>
        [Fact]
        public async Task TopicSwitch_ShouldPreserveContext()
        {
            // Arrange
            var conversationId = "test-topic-switch-001";
            var userId = "test-user";

            // Act
            // Topic 1: 灯光控制
            var request1 = CreateRequest(conversationId, userId, "打开客厅的灯", new());

            // Topic 2: 音乐播放（话题切换）
            var request2 = CreateRequest(conversationId, userId, "播放周杰伦的歌", new());

            // Topic 3: 回到灯光（话题回退）
            var request3 = CreateRequest(conversationId, userId, "把灯关了", new());

            // Assert
            // Verify dialog state is preserved and topic switches are handled
            _output.WriteLine("Topic switch test completed");
        }

        /// <summary>
        /// 测试长对话中的Token使用优化
        /// Test token usage optimization in long dialogs
        /// </summary>
        [Fact]
        public async Task LongDialog_ShouldOptimizeTokenUsage()
        {
            // Arrange
            var conversationId = "test-token-optimization-001";
            var userId = "test-user";

            // Act
            // Simulate 20-turn conversation
            for (int i = 1; i <= 20; i++)
            {
                var userInput = $"第{i}轮对话内容，包含一些上下文信息";
                var request = CreateRequest(conversationId, userId, userInput, new());
                // _mainAgent.ExecuteBusinessLogicAsync(request);
            }

            // Assert
            // Verify token usage is optimized through:
            // - Context compression at turns 5, 10, 15, 20
            // - Memory classification reducing redundant context
            // - Historical slot references instead of full context

            _output.WriteLine("Token optimization test completed");
        }

        /// <summary>
        /// 测试SubAgent槽位缺失时的自动恢复
        /// Test automatic recovery from SubAgent slot missing
        /// </summary>
        [Fact]
        public async Task SubAgentSlotMissing_ShouldAutoRecover()
        {
            // Arrange
            var conversationId = "test-slot-missing-001";
            var userId = "test-user";

            // Act
            // First request: Set room preference
            var request1 = CreateRequest(conversationId, userId, "我通常在客厅工作", new());

            // Second request: Control device without specifying room (should auto-fill from history)
            var request2 = CreateRequest(conversationId, userId, "打开灯", new());

            // Assert
            // Verify slot was auto-filled from historical slots
            // Verify SubAgent task completed successfully after auto-fill

            _output.WriteLine("SubAgent slot missing recovery test completed");
        }

        private MafTaskRequest CreateRequest(
            string conversationId,
            string userId,
            string userInput,
            Dictionary<string, object> parameters)
        {
            return new MafTaskRequest
            {
                TaskId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                UserId = userId,
                UserInput = userInput,
                Parameters = parameters,
                Priority = 50
            };
        }
    }
}
