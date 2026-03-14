using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Infrastructure.Context
{
    /// <summary>
    /// 上下文压缩提供器
    /// 当上下文过长时，智能压缩历史对话以节省 Token
    /// </summary>
    /// <remarks>
    /// 功能特性：
    /// - Token 估算：根据文本长度估算 Token 数量
    /// - 智能压缩：使用 LLM 总结历史对话，保留关键信息
    /// - 分层压缩：保留近期完整历史，远期历史压缩为摘要
    /// - 降级策略：压缩失败时返回原始上下文
    ///
    /// 压缩策略：
    /// - Level 1 (0-3000 tokens): 不压缩，直接使用
    /// - Level 2 (3000-6000 tokens): 压缩最旧的 50%
    /// - Level 3 (6000-9000 tokens): 压缩最旧的 70%
    /// - Level 4 (>9000 tokens): 压缩最旧的 90%
    ///
    /// 使用场景：
    /// - 长对话系统
    /// - 文档分析工具
    /// - 代码审查助手
    /// - 知识库问答
    /// </remarks>
    public class ContextCompressionProvider : IAIContextProvider
    {
        private readonly ILogger<ContextCompressionProvider> _logger;
        private readonly ContextCompressionOptions _options;
        private readonly ILLMCompressionService? _compressionService;

        /// <summary>
        /// 构造函数（无 LLM 压缩服务）
        /// </summary>
        public ContextCompressionProvider(
            ILogger<ContextCompressionProvider> logger,
            ContextCompressionOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new ContextCompressionOptions();
        }

        /// <summary>
        /// 构造函数（带 LLM 压缩服务）
        /// </summary>
        public ContextCompressionProvider(
            ILogger<ContextCompressionProvider> logger,
            ILLMCompressionService compressionService,
            ContextCompressionOptions? options = null)
            : this(logger, options)
        {
            _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
        }

        /// <summary>
        /// 在 LLM 调用前压缩上下文
        /// </summary>
        public async Task<AIContext> PrepareContextAsync(AIContext currentContext, CancellationToken cancellationToken)
        {
            try
            {
                // 估算当前上下文的 Token 数量
                var currentTokenCount = EstimateTokenCount(currentContext);

                if (currentTokenCount <= _options.MaxTokens)
                {
                    _logger.LogDebug(
                        "[ContextCompression] Token 数量 {Count} 在阈值内，无需压缩",
                        currentTokenCount);
                    return currentContext;
                }

                _logger.LogInformation(
                    "[ContextCompression] Token 数量 {Count} 超过阈值 {MaxTokens}，开始压缩",
                    currentTokenCount,
                    _options.MaxTokens);

                // 执行压缩
                var compressedContext = await CompressContextAsync(currentContext, cancellationToken);

                var newTokenCount = EstimateTokenCount(compressedContext);
                _logger.LogInformation(
                    "[ContextCompression] 压缩完成：{OldCount} -> {NewCount} tokens (减少 {Reduction}%)",
                    currentTokenCount,
                    newTokenCount,
                    100 * (currentTokenCount - newTokenCount) / currentTokenCount);

                return compressedContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ContextCompression] 压缩失败，返回原始上下文");
                return currentContext; // 降级：返回原始上下文
            }
        }

        /// <summary>
        /// 在 LLM 调用后处理响应（可选）
        /// </summary>
        public async Task<AIContext> ProcessContextAsync(AIContext context, AIResult result, CancellationToken cancellationToken)
        {
            // 可以在这里记录压缩统计信息
            await Task.CompletedTask;
            return context;
        }

        /// <summary>
        /// 压缩上下文
        /// </summary>
        private async Task<AIContext> CompressContextAsync(AIContext context, CancellationToken ct)
        {
            var messages = context.Messages.ToList();
            var totalTokens = EstimateTokenCount(context);

            // 根据超过程度决定压缩策略
            var excessRatio = (double)totalTokens / _options.MaxTokens;

            if (excessRatio <= 1.5)
            {
                // Level 2: 压缩最旧的 50%
                return await CompressOldestMessagesAsync(context, 0.5, ct);
            }
            else if (excessRatio <= 2.0)
            {
                // Level 3: 压缩最旧的 70%
                return await CompressOldestMessagesAsync(context, 0.7, ct);
            }
            else
            {
                // Level 4: 压缩最旧的 90%
                return await CompressOldestMessagesAsync(context, 0.9, ct);
            }
        }

        /// <summary>
        /// 压缩最旧的消息
        /// </summary>
        private async Task<AIContext> CompressOldestMessagesAsync(
            AIContext context,
            double compressionRatio,
            CancellationToken ct)
        {
            var messages = context.Messages.ToList();
            if (messages.Count == 0)
            {
                return context;
            }

            // 计算要压缩的消息数量
            var compressCount = (int)(messages.Count * compressionRatio);
            if (compressCount < 1)
            {
                compressCount = 1;
            }

            // 分离要压缩的消息和保留的消息
            var toCompress = messages.Take(compressCount).ToList();
            var toKeep = messages.Skip(compressCount).ToList();

            // 压缩消息
            string compressedSummary;
            if (_compressionService != null)
            {
                // 使用 LLM 进行智能压缩
                compressedSummary = await _compressionService.SummarizeAsync(toCompress, ct);
            }
            else
            {
                // 使用简单规则压缩
                compressedSummary = SimpleSummarize(toCompress);
            }

            // 创建压缩后的消息
            var compressedMessage = new ChatMessage(
                ChatRole.System,
                $"[历史对话摘要]\n{compressedSummary}");

            // 组合消息：压缩摘要 + 保留的消息
            var newMessages = new List<ChatMessage> { compressedMessage };
            newMessages.AddRange(toKeep);

            // 返回新的上下文
            return context with { Messages = newMessages };
        }

        /// <summary>
        /// 简单规则压缩（不使用 LLM）
        /// </summary>
        private string SimpleSummarize(List<ChatMessage> messages)
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"历史对话包含 {messages.Count / 2} 轮交互：");

            foreach (var msg in messages)
            {
                var role = msg.Role == ChatRole.User ? "用户" : "助手";
                var content = msg.Text ?? string.Empty;
                var preview = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
                summary.AppendLine($"- {role}: {preview}");
            }

            return summary.ToString();
        }

        /// <summary>
        /// 估算上下文的 Token 数量
        /// </summary>
        private int EstimateTokenCount(AIContext context)
        {
            // 中文：约 1.5 characters per token
            // 英文：约 4 characters per token
            // 这里使用保守估计：1 token ≈ 3 characters

            var totalChars = 0;

            // 系统消息
            if (!string.IsNullOrWhiteSpace(context.SystemMessage))
            {
                totalChars += context.SystemMessage.Length;
            }

            // 对话消息
            foreach (var msg in context.Messages)
            {
                if (msg.Text != null)
                {
                    totalChars += msg.Text.Length;
                }
            }

            // 保守估算：除以 3
            return (int)Math.Ceiling(totalChars / 3.0);
        }

        /// <summary>
        /// 获取压缩统计信息
        /// </summary>
        public CompressionStats GetStats()
        {
            // TODO: 实现统计信息收集
            return new CompressionStats
            {
                TotalCompressions = 0,
                AverageCompressionRatio = 0.0,
                LastCompressionTime = DateTime.MinValue
            };
        }
    }

    /// <summary>
    /// 上下文压缩配置选项
    /// </summary>
    public class ContextCompressionOptions
    {
        /// <summary>
        /// 最大 Token 数量阈值（默认 4000）
        /// </summary>
        public int MaxTokens { get; set; } = 4000;

        /// <summary>
        /// 是否启用压缩（默认 true）
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// 压缩模式（默认 Smart）
        /// </summary>
        public CompressionMode Mode { get; set; } = CompressionMode.Smart;

        /// <summary>
        /// 最小保留消息数（默认 2，即至少保留 1 轮对话）
        /// </summary>
        public int MinMessagesToKeep { get; set; } = 2;
    }

    /// <summary>
    /// 压缩模式
    /// </summary>
    public enum CompressionMode
    {
        /// <summary>
        /// 简单模式：仅截断，不总结
        /// </summary>
        Simple,

        /// <summary>
        /// 智能模式：使用 LLM 总结
        /// </summary>
        Smart
    }

    /// <summary>
    /// 压缩统计信息
    /// </summary>
    public class CompressionStats
    {
        /// <summary>
        /// 总压缩次数
        /// </summary>
        public int TotalCompressions { get; set; }

        /// <summary>
        /// 平均压缩比例
        /// </summary>
        public double AverageCompressionRatio { get; set; }

        /// <summary>
        /// 最后一次压缩时间
        /// </summary>
        public DateTime LastCompressionTime { get; set; }
    }

    /// <summary>
    /// LLM 压缩服务接口
    /// </summary>
    public interface ILLMCompressionService
    {
        /// <summary>
        /// 总结消息列表
        /// </summary>
        Task<string> SummarizeAsync(List<ChatMessage> messages, CancellationToken cancellationToken);
    }

    /// <summary>
    /// 基于 LLM 的压缩服务实现
    /// </summary>
    public class LLMCompressionService : ILLMCompressionService
    {
        private readonly Core.Abstractions.IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<LLMCompressionService> _logger;

        public LLMCompressionService(
            Core.Abstractions.IMafAiAgentRegistry llmRegistry,
            ILogger<LLMCompressionService> logger)
        {
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SummarizeAsync(List<ChatMessage> messages, CancellationToken ct)
        {
            try
            {
                // 构建总结提示词
                var prompt = BuildSummaryPrompt(messages);

                // 调用 LLM
                var agent = await _llmRegistry.GetBestAgentAsync(Core.Models.LLM.LlmScenario.Chat, ct);
                var summary = await agent.ExecuteAsync(
                    agent.GetCurrentModelId(),
                    prompt,
                    "你是一个专业的对话总结助手。请简洁地总结对话历史。",
                    ct);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LLMCompression] LLM 总结失败");
                throw;
            }
        }

        private string BuildSummaryPrompt(List<ChatMessage> messages)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("请总结以下对话历史，提取关键信息：");
            sb.AppendLine();

            foreach (var msg in messages)
            {
                var role = msg.Role == ChatRole.User ? "用户" : "助手";
                sb.AppendLine($"{role}: {msg.Text}");
            }

            sb.AppendLine();
            sb.AppendLine("请用简洁的语言总结上述对话的主要内容和结论。");

            return sb.ToString();
        }
    }
}
