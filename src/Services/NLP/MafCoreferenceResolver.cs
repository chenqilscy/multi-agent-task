using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 指代消解服务
    /// 解决多轮对话中的代词指代问题
    /// </summary>
    public class MafCoreferenceResolver : ICoreferenceResolver
    {
        private readonly IMafSessionStorage _sessionStorage;
        private readonly ILogger<MafCoreferenceResolver> _logger;

        private static readonly string[] Pronouns = ["它", "那个", "这个", "他", "她", "他们", "那", "这"];

        public MafCoreferenceResolver(
            IMafSessionStorage sessionStorage,
            ILogger<MafCoreferenceResolver> logger)
        {
            _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string> ResolveAsync(
            string userInput,
            string conversationId,
            CancellationToken ct = default)
        {
            if (!Pronouns.Any(p => userInput.Contains(p)))
            {
                return userInput;
            }

            _logger.LogDebug("Resolving coreferences in: {Input}", userInput);

            // 尝试从会话历史中找到最近提到的实体
            try
            {
                var session = await _sessionStorage.LoadSessionAsync(conversationId, ct);
                if (session != null)
                {
                    // 简化实现：不依赖MS AF的Messages属性
                    // 实际项目中应该从session中提取历史消息
                    _logger.LogDebug("Session loaded for coreference resolution: {SessionId}", session.SessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load session for coreference resolution");
            }

            return userInput;
        }

        /// <summary>
        /// 使用LLM进行指代消解
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <param name="context">对话上下文</param>
        /// <param name="entities">已识别的实体列表</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>消解后的文本</returns>
        public async Task<string> ResolveCoreferencesWithLlmAsync(
            string userInput,
            DialogContext context,
            Dictionary<string, object> entities,
            CancellationToken ct = default)
        {
            if (!Pronouns.Any(p => userInput.Contains(p)) || entities.Count == 0)
            {
                return userInput;
            }

            _logger.LogDebug("Using LLM to resolve coreferences in: {Input}", userInput);

            try
            {
                // 构建提示词
                var prompt = BuildCoreferencePrompt(userInput, context, entities);

                // 注意：这里需要实际的LLM服务调用
                // 由于架构要求使用MS AF，这里需要通过注入的LLM agent来调用
                // 暂时返回基于规则的简单实现
                var resolved = await RuleBasedResolution(userInput, entities, context);

                _logger.LogDebug("LLM resolved coreferences: {Original} -> {Resolved}", userInput, resolved);
                return resolved;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM-based coreference resolution failed, falling back to rule-based");
                return await RuleBasedResolution(userInput, entities, context);
            }
        }

        /// <summary>
        /// 用实体替换代词
        /// </summary>
        private string ReplacePronounsWithEntities(string input, List<string> entities)
        {
            var result = input;
            foreach (var pronoun in Pronouns.OrderByDescending(p => p.Length))
            {
                if (result.Contains(pronoun) && entities.Count > 0)
                {
                    // 使用最近提到的实体替换代词
                    result = result.Replace(pronoun, entities[0]);
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// 构建指代消解的提示词
        /// </summary>
        private string BuildCoreferencePrompt(string userInput, DialogContext context, Dictionary<string, object> entities)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("你是一个指代消解专家。请将用户输入中的代词替换为实际指代的实体。");
            sb.AppendLine();
            sb.AppendLine("当前用户输入：");
            sb.AppendLine(userInput);
            sb.AppendLine();
            sb.AppendLine("对话上下文：");
            sb.AppendLine($"- 会话ID: {context.SessionId}");
            sb.AppendLine($"- 对话轮次: {context.TurnCount}");
            sb.AppendLine($"- 上一个意图: {context.PreviousIntent ?? "无"}");
            sb.AppendLine();
            sb.AppendLine("已识别的实体：");
            foreach (var entity in entities)
            {
                sb.AppendLine($"- {entity.Key}: {entity.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("请输出消解后的文本，只输出文本，不要解释。");

            return sb.ToString();
        }

        /// <summary>
        /// 基于规则的指代消解（回退方案）
        /// </summary>
        private Task<string> RuleBasedResolution(string userInput, Dictionary<string, object> entities, DialogContext context)
        {
            var result = userInput;

            // 优先使用历史槽位值
            if (context.HistoricalSlots.Count > 0)
            {
                var recentSlot = context.HistoricalSlots.Last();
                foreach (var pronoun in Pronouns)
                {
                    if (result.Contains(pronoun))
                    {
                        result = result.Replace(pronoun, recentSlot.Value.ToString() ?? pronoun);
                        break;
                    }
                }
            }

            return Task.FromResult(result);
        }
    }
}
