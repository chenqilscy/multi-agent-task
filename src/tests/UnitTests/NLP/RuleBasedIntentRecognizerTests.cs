using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    /// <summary>
    /// 测试用的意图关键词提供者
    /// </summary>
    public class TestIntentKeywordProvider : IIntentKeywordProvider
    {
        private readonly Dictionary<string, string[]> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ControlLight"] = ["灯", "照明", "亮", "暗", "开灯", "关灯"],
            ["AdjustClimate"] = ["温度", "空调", "冷", "热", "暖", "制冷", "制热"],
            ["PlayMusic"] = ["音乐", "播放", "歌曲", "歌", "音频"],
            ["SecurityControl"] = ["门", "锁", "安全", "门锁", "摄像头"],
            ["GeneralQuery"] = ["查询", "状态", "怎么", "什么", "帮我"]
        };

        public string?[]? GetKeywords(string intent)
        {
            _map.TryGetValue(intent, out var keywords);
            return keywords;
        }

        public IEnumerable<string> GetSupportedIntents()
        {
            return _map.Keys;
        }
    }

    public class RuleBasedIntentRecognizerTests
    {
        private readonly RuleBasedIntentRecognizer _sut;
        private readonly TestIntentKeywordProvider _keywordProvider;

        public RuleBasedIntentRecognizerTests()
        {
            _keywordProvider = new TestIntentKeywordProvider();
            _sut = new RuleBasedIntentRecognizer(_keywordProvider, NullLogger<RuleBasedIntentRecognizer>.Instance);
        }

        [Fact]
        public async Task RecognizeAsync_WhenLightKeyword_ShouldReturnControlLightIntent()
        {
            // Arrange
            var input = "打开客厅的灯";

            // Act
            var result = await _sut.RecognizeAsync(input);

            // Assert
            result.PrimaryIntent.Should().Be("ControlLight");
            result.Confidence.Should().BeGreaterThan(0);
            result.OriginalInput.Should().Be(input);
        }

        [Fact]
        public async Task RecognizeAsync_WhenTemperatureKeyword_ShouldReturnAdjustClimateIntent()
        {
            // Arrange
            var input = "把温度调到26度";

            // Act
            var result = await _sut.RecognizeAsync(input);

            // Assert
            result.PrimaryIntent.Should().Be("AdjustClimate");
            result.Confidence.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RecognizeAsync_WhenMusicKeyword_ShouldReturnPlayMusicIntent()
        {
            // Arrange
            var input = "播放音乐";

            // Act
            var result = await _sut.RecognizeAsync(input);

            // Assert
            result.PrimaryIntent.Should().Be("PlayMusic");
            result.Confidence.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RecognizeAsync_WhenUnknownInput_ShouldReturnUnknownIntent()
        {
            // Arrange
            var input = "这是一个完全随机的输入";

            // Act
            var result = await _sut.RecognizeAsync(input);

            // Assert
            result.PrimaryIntent.Should().Be("Unknown");
            result.Confidence.Should().Be(0.0);
        }

        [Fact]
        public async Task RecognizeBatchAsync_ShouldReturnResultForEachInput()
        {
            // Arrange
            var inputs = new List<string>
            {
                "打开灯",
                "调节温度",
                "播放音乐"
            };

            // Act
            var results = await _sut.RecognizeBatchAsync(inputs);

            // Assert
            results.Should().HaveCount(3);
            results[0].PrimaryIntent.Should().Be("ControlLight");
            results[1].PrimaryIntent.Should().Be("AdjustClimate");
            results[2].PrimaryIntent.Should().Be("PlayMusic");
        }
    }
}
