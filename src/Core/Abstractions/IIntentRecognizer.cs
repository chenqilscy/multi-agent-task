using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图识别结果
    /// </summary>
    public class IntentRecognitionResult
    {
        /// <summary>主要意图</summary>
        public string PrimaryIntent { get; set; } = string.Empty;

        /// <summary>识别置信度（0.0-1.0）</summary>
        public double Confidence { get; set; }

        /// <summary>备选意图和置信度字典</summary>
        public Dictionary<string, double> AlternativeIntents { get; set; } = new();

        /// <summary>标签列表</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>原始用户输入</summary>
        public string OriginalInput { get; set; } = string.Empty;
    }

    /// <summary>
    /// 意图识别器接口
    /// </summary>
    public interface IIntentRecognizer
    {
        /// <summary>
        /// 识别用户意图
        /// </summary>
        Task<IntentRecognitionResult> RecognizeAsync(
            string userInput,
            CancellationToken ct = default);

        /// <summary>
        /// 批量识别意图
        /// </summary>
        Task<List<IntentRecognitionResult>> RecognizeBatchAsync(
            List<string> userInputs,
            CancellationToken ct = default);
    }
}
