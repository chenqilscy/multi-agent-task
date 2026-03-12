using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 意图识别器（组合规则识别和向量识别）
    /// </summary>
    public class MafIntentRecognizer : IIntentRecognizer
    {
        private readonly IIntentRecognizer _primaryRecognizer;
        private readonly ILogger<MafIntentRecognizer> _logger;

        public MafIntentRecognizer(
            RuleBasedIntentRecognizer primaryRecognizer,
            ILogger<MafIntentRecognizer> logger)
        {
            _primaryRecognizer = primaryRecognizer ?? throw new ArgumentNullException(nameof(primaryRecognizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IntentRecognitionResult> RecognizeAsync(
            string userInput,
            CancellationToken ct = default)
        {
            _logger.LogDebug("MafIntentRecognizer recognizing: {Input}", userInput);
            return await _primaryRecognizer.RecognizeAsync(userInput, ct);
        }

        /// <inheritdoc />
        public async Task<List<IntentRecognitionResult>> RecognizeBatchAsync(
            List<string> userInputs,
            CancellationToken ct = default)
        {
            return await _primaryRecognizer.RecognizeBatchAsync(userInputs, ct);
        }
    }
}
