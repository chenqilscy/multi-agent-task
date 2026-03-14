using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    /// <summary>
    /// 测试用的实体模式提供者
    /// </summary>
    public class TestEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Room"] = ["客厅", "卧室", "厨房"],
            ["Device"] = ["灯", "空调", "电视"],
            ["Action"] = ["打开", "关闭", "调节"]
        };

        public string?[]? GetPatterns(string entityType)
        {
            _map.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _map.Keys;
        }

        public string GetFewShotExamples()
        {
            return "Example few-shot content for testing";
        }
    }

    public class MafEntityExtractorTests
    {
        private readonly MafEntityExtractor _sut;
        private readonly TestEntityPatternProvider _patternProvider;

        public MafEntityExtractorTests()
        {
            _patternProvider = new TestEntityPatternProvider();
            _sut = new MafEntityExtractor(_patternProvider, NullLogger<MafEntityExtractor>.Instance);
        }

        [Fact]
        public async Task ExtractAsync_WhenRoomKeyword_ShouldExtractRoomEntity()
        {
            // Arrange
            var input = "打开客厅的灯";

            // Act
            var result = await _sut.ExtractAsync(input);

            // Assert
            result.Should().NotBeNull();
            result.ExtractedEntities.Should().NotBeEmpty();
            result.ExtractedEntities.Should().Contain(e => e.EntityType == "Room");
            result.ExtractedEntities.Should().Contain(e => e.EntityType == "Device");
            result.Entities.Should().ContainKey("Room");
            result.Entities["Room"].Should().Be("客厅");
        }

        [Fact]
        public async Task ExtractAsync_WhenDeviceKeyword_ShouldExtractDeviceEntity()
        {
            // Arrange
            var input = "调节空调温度";

            // Act
            var result = await _sut.ExtractAsync(input);

            // Assert
            result.Should().NotBeNull();
            result.ExtractedEntities.Should().NotBeEmpty();
            result.ExtractedEntities.Should().Contain(e => e.EntityType == "Device" && e.EntityValue == "空调");
            result.Entities["Device"].Should().Be("空调");
        }

        [Fact]
        public async Task ExtractAsync_WhenActionKeyword_ShouldExtractActionEntity()
        {
            // Arrange
            var input = "关闭电视";

            // Act
            var result = await _sut.ExtractAsync(input);

            // Assert
            result.Should().NotBeNull();
            result.ExtractedEntities.Should().NotBeEmpty();
            result.ExtractedEntities.Should().Contain(e => e.EntityType == "Action" && e.EntityValue == "关闭");
            result.Entities["Action"].Should().Be("关闭");
        }

        [Fact]
        public async Task ExtractAsync_WhenMultipleKeywords_ShouldExtractAllEntities()
        {
            // Arrange
            var input = "打开卧室的空调";

            // Act
            var result = await _sut.ExtractAsync(input);

            // Assert
            result.Should().NotBeNull();
            result.ExtractedEntities.Count.Should().BeGreaterThanOrEqualTo(2);
            result.Entities.Should().ContainKey("Room");
            result.Entities.Should().ContainKey("Device");
            result.Entities["Room"].Should().Be("卧室");
            result.Entities["Device"].Should().Be("空调");
        }

        [Fact]
        public async Task ExtractAsync_WhenNoKeyword_ShouldReturnEmptyResult()
        {
            // Arrange
            var input = "这是一个完全随机的输入";

            // Act
            var result = await _sut.ExtractAsync(input);

            // Assert
            result.Should().NotBeNull();
            result.ExtractedEntities.Should().BeEmpty();
            result.Entities.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractAsync_ShouldSetCorrectPositions()
        {
            // Arrange
            var input = "打开客厅的灯";

            // Act
            var result = await _sut.ExtractAsync(input);

            // Assert
            result.Should().NotBeNull();
            var roomEntity = result.ExtractedEntities.FirstOrDefault(e => e.EntityType == "Room");
            roomEntity.Should().NotBeNull();
            roomEntity!.StartPosition.Should().Be(2); // "打开" 之后
            roomEntity.EndPosition.Should().BeGreaterThan(roomEntity.StartPosition);
            roomEntity.Confidence.Should().Be(0.9);
        }
    }
}
