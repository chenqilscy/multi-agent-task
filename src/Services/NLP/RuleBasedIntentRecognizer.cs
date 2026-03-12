using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 基于规则的意图识别器
    /// 使用关键词匹配进行简单意图识别
    /// </summary>
    public class RuleBasedIntentRecognizer : IIntentRecognizer
    {
        private readonly ILogger<RuleBasedIntentRecognizer> _logger;

        private static readonly Dictionary<string, string[]> IntentKeywords = new()
        {
            ["ControlLight"] = ["灯", "照明", "亮", "暗", "开灯", "关灯"],
            ["AdjustClimate"] = ["温度", "空调", "冷", "热", "暖", "制冷", "制热"],
            ["PlayMusic"] = ["音乐", "播放", "歌曲", "歌", "音频"],
            ["SecurityControl"] = ["门", "锁", "安全", "门锁", "摄像头"],
            ["GeneralQuery"] = ["查询", "状态", "怎么", "什么", "帮我"]
        };

        public RuleBasedIntentRecognizer(ILogger<RuleBasedIntentRecognizer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<IntentRecognitionResult> RecognizeAsync(
            string userInput,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Recognizing intent for input: {Input}", userInput);

            var result = new IntentRecognitionResult
            {
                OriginalInput = userInput
            };

            var scores = new Dictionary<string, double>();
            foreach (var (intent, keywords) in IntentKeywords)
            {
                var matchCount = keywords.Count(k => userInput.Contains(k, StringComparison.OrdinalIgnoreCase));
                if (matchCount > 0)
                {
                    scores[intent] = (double)matchCount / keywords.Length;
                }
            }

            if (scores.Count > 0)
            {
                var sorted = scores.OrderByDescending(x => x.Value).ToList();
                result.PrimaryIntent = sorted[0].Key;
                result.Confidence = sorted[0].Value;
                result.AlternativeIntents = sorted.Skip(1).ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                result.PrimaryIntent = "Unknown";
                result.Confidence = 0.0;
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public async Task<List<IntentRecognitionResult>> RecognizeBatchAsync(
            List<string> userInputs,
            CancellationToken ct = default)
        {
            var tasks = userInputs.Select(input => RecognizeAsync(input, ct));
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}
