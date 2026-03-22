using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Services.Dialog;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Dialog
{
    public class MemoryClassifierTests
    {
        private readonly Mock<IMafMemoryManager> _mockMemory = new();
        private readonly Mock<ILogger<MemoryClassifier>> _mockLogger = new();
        private readonly MemoryClassifier _classifier;

        public MemoryClassifierTests()
        {
            _classifier = new MemoryClassifier(_mockMemory.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullMemoryManager_ShouldThrow()
        {
            var act = () => new MemoryClassifier(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var act = () => new MemoryClassifier(_mockMemory.Object, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ClassifyAndStoreAsync_HighFrequencySlot_ShouldStoreAsLongTerm()
        {
            var context = new DialogContext
            {
                UserId = "user-1",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["light_control.room"] = 5 // >= 3 times
                }
            };
            var slots = new Dictionary<string, object>
            {
                ["room"] = "客厅"
            };

            var result = await _classifier.ClassifyAndStoreAsync("light_control", slots, context);

            result.LongTermMemories.Should().HaveCount(1);
            result.LongTermMemories[0].Key.Should().Be("light_control.room");
            result.LongTermMemories[0].Value.Should().Be("客厅");
            result.LongTermMemories[0].ImportanceScore.Should().Be(0.8);

            _mockMemory.Verify(m => m.SaveSemanticMemoryAsync(
                "user-1",
                "light_control.room",
                "客厅",
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ClassifyAndStoreAsync_LowFrequencySlot_ShouldStoreAsShortTerm()
        {
            var context = new DialogContext
            {
                UserId = "user-1",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["light_control.room"] = 1 // < 3 times
                }
            };
            var slots = new Dictionary<string, object>
            {
                ["room"] = "卧室"
            };

            var result = await _classifier.ClassifyAndStoreAsync("light_control", slots, context);

            result.ShortTermMemories.Should().HaveCount(1);
            result.ShortTermMemories[0].Key.Should().Be("light_control.room");
            result.LongTermMemories.Should().BeEmpty();
        }

        [Fact]
        public async Task ClassifyAndStoreAsync_NewSlot_ShouldStoreAsShortTerm()
        {
            var context = new DialogContext
            {
                UserId = "user-1",
                HistoricalSlots = new Dictionary<string, object>()
            };
            var slots = new Dictionary<string, object>
            {
                ["temperature"] = 26
            };

            var result = await _classifier.ClassifyAndStoreAsync("climate_control", slots, context);

            result.ShortTermMemories.Should().HaveCount(1);
            result.LongTermMemories.Should().BeEmpty();
        }

        [Fact]
        public async Task ClassifyAndStoreAsync_MultipleSlots_ShouldClassifyEach()
        {
            var context = new DialogContext
            {
                UserId = "user-1",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["intent.room"] = 5,
                    ["intent.device"] = 1
                }
            };
            var slots = new Dictionary<string, object>
            {
                ["room"] = "客厅",
                ["device"] = "灯"
            };

            var result = await _classifier.ClassifyAndStoreAsync("intent", slots, context);

            result.LongTermMemories.Should().HaveCount(1);
            result.ShortTermMemories.Should().HaveCount(1);
        }

        // ========== EvaluateForgetting ==========

        [Fact]
        public void EvaluateForgetting_RecentlyAccessedFrequently_ShouldKeep()
        {
            var memory = new SemanticMemory { Key = "test" };

            var decision = _classifier.EvaluateForgetting(
                memory,
                lastAccessed: DateTime.UtcNow.AddDays(-5),
                accessCount: 20);

            decision.Should().Be(ForgettingDecision.Keep);
        }

        [Fact]
        public void EvaluateForgetting_30DaysOld_HighAccess_ShouldDowngrade()
        {
            var memory = new SemanticMemory { Key = "test" };

            var decision = _classifier.EvaluateForgetting(
                memory,
                lastAccessed: DateTime.UtcNow.AddDays(-31),
                accessCount: 15);

            decision.Should().Be(ForgettingDecision.Downgrade);
        }

        [Fact]
        public void EvaluateForgetting_30DaysOld_LowAccess_ShouldDelete()
        {
            var memory = new SemanticMemory { Key = "test" };

            var decision = _classifier.EvaluateForgetting(
                memory,
                lastAccessed: DateTime.UtcNow.AddDays(-31),
                accessCount: 5);

            decision.Should().Be(ForgettingDecision.Delete);
        }
    }
}
