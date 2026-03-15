using CKY.MultiAgentFramework.Core.Models.Dialog;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 意图漂移检测器
    /// 检测用户是否在对话中改变了话题
    /// Intent drift detector - detects if user changed topic during conversation
    /// </summary>
    public class IntentDriftDetector
    {
        private readonly ILogger<IntentDriftDetector> _logger;
        private readonly string[] _topicSwitchTriggers;

        public IntentDriftDetector(
            IEnumerable<string>? topicSwitchTriggers,
            ILogger<IntentDriftDetector> logger)
        {
            // Fixed: Make topic triggers configurable via constructor injection
            _topicSwitchTriggers = topicSwitchTriggers?.ToArray()
                ?? new[] { "对了", "另外", "顺便", "还有", "对了再说", "另外问一下" };
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检测意图漂移
        /// </summary>
        /// <param name="currentInput">当前用户输入</param>
        /// <param name="previousIntent">上一个意图</param>
        /// <param name="context">对话上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>意图漂移分析结果</returns>
        public Task<IntentDriftAnalysis> DetectDriftAsync(
            string currentInput,
            string previousIntent,
            DialogContext context,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Detecting intent drift. Previous: {Previous}, Current: {Current}", previousIntent, currentInput);

            var analysis = new IntentDriftAnalysis
            {
                CurrentInput = currentInput,
                PreviousIntent = previousIntent,
                HasDrifted = false,
                DriftScore = 0.0,
                SuggestedAction = DriftAction.Continue
            };

            try
            {
                // 1. 检查话题转换触发词
                if (HasTopicSwitchTrigger(currentInput))
                {
                    analysis.HasDrifted = true;
                    analysis.DriftScore = 0.9;
                    analysis.SuggestedAction = DriftAction.NewTopic;
                    analysis.Reason = "检测到话题转换触发词";
                    _logger.LogInformation("Topic switch trigger detected in input: {Input}", currentInput);
                    return Task.FromResult(analysis);
                }

                // 2. 基于历史槽位判断语义相似度
                if (context.HistoricalSlots.Count > 0)
                {
                    var semanticScore = CalculateSemanticSimilarity(currentInput, context);
                    analysis.SemanticSimilarityScore = semanticScore;

                    if (semanticScore < 0.3) // 语义相似度低，可能改变了话题
                    {
                        analysis.HasDrifted = true;
                        analysis.DriftScore = 1.0 - semanticScore;
                        analysis.SuggestedAction = DriftAction.PossibleNewTopic;
                        analysis.Reason = "语义相似度较低";
                    }
                }

                // 3. 基于对话轮次判断（长时间对话容易漂移）
                if (context.TurnCount > 10)
                {
                    analysis.DriftScore += 0.1;
                    analysis.Reason += "；对话轮次较多";
                }

                // 4. 确定最终的漂移状态和建议动作
                if (analysis.DriftScore > 0.7)
                {
                    analysis.SuggestedAction = DriftAction.NewTopic;
                }
                else if (analysis.DriftScore > 0.4)
                {
                    analysis.SuggestedAction = DriftAction.PossibleNewTopic;
                }

                _logger.LogDebug("Intent drift analysis complete. Score: {Score}, Action: {Action}",
                    analysis.DriftScore, analysis.SuggestedAction);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect intent drift");
                analysis.SuggestedAction = DriftAction.Continue;
            }

            return Task.FromResult(analysis);
        }

        /// <summary>
        /// 检查是否包含话题转换触发词
        /// </summary>
        private bool HasTopicSwitchTrigger(string input)
        {
            return _topicSwitchTriggers.Any(trigger => input.Contains(trigger));
        }

        /// <summary>
        /// 计算语义相似度（简化版）
        /// 实际项目中应使用向量相似度计算
        /// </summary>
        private double CalculateSemanticSimilarity(string currentInput, DialogContext context)
        {
            // 简单的关键词重叠度计算
            var currentWords = currentInput.Split(new[] { ' ', '，', '。', '？', '！' }, StringSplitOptions.RemoveEmptyEntries);
            var historicalWords = context.HistoricalSlots
                .SelectMany(kvp => kvp.Value.ToString()?.Split(new[] { ' ', '，', '。', '？', '！' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                .Distinct();

            if (currentWords.Length == 0 || historicalWords.Count() == 0)
            {
                return 0.0;
            }

            var overlapCount = currentWords.Intersect(historicalWords, StringComparer.OrdinalIgnoreCase).Count();
            var similarity = (double)overlapCount / Math.Max(currentWords.Length, historicalWords.Count());

            return similarity;
        }
    }

    /// <summary>
    /// 意图漂移分析结果
    /// </summary>
    public class IntentDriftAnalysis
    {
        /// <summary>
        /// 当前用户输入
        /// </summary>
        public string CurrentInput { get; set; } = string.Empty;

        /// <summary>
        /// 上一个意图
        /// </summary>
        public string PreviousIntent { get; set; } = string.Empty;

        /// <summary>
        /// 是否发生意图漂移
        /// </summary>
        public bool HasDrifted { get; set; }

        /// <summary>
        /// 漂移分数（0-1，1表示完全改变话题）
        /// </summary>
        public double DriftScore { get; set; }

        /// <summary>
        /// 语义相似度分数（0-1，1表示语义完全相同）
        /// </summary>
        public double SemanticSimilarityScore { get; set; }

        /// <summary>
        /// 建议的动作
        /// </summary>
        public DriftAction SuggestedAction { get; set; }

        /// <summary>
        /// 原因说明
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 漂移动作建议
    /// </summary>
    public enum DriftAction
    {
        /// <summary>
        /// 继续当前话题
        /// </summary>
        Continue,

        /// <summary>
        /// 可能的新话题（需要确认）
        /// </summary>
        PossibleNewTopic,

        /// <summary>
        /// 确定是新话题
        /// </summary>
        NewTopic
    }
}
