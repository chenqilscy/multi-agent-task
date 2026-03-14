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
        private readonly IIntentKeywordProvider _keywordProvider;

        public RuleBasedIntentRecognizer(
            IIntentKeywordProvider keywordProvider,
            ILogger<RuleBasedIntentRecognizer> logger)
        {
            _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
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
            var supportedIntents = _keywordProvider.GetSupportedIntents();

            foreach (var intent in supportedIntents)
            {
                var keywords = _keywordProvider.GetKeywords(intent);
                if (keywords != null)
                {
                    var matchCount = keywords.Count(k => !string.IsNullOrEmpty(k) && userInput.Contains(k, StringComparison.OrdinalIgnoreCase));
                    if (matchCount > 0)
                    {
                        scores[intent] = (double)matchCount / keywords.Length;
                    }
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
