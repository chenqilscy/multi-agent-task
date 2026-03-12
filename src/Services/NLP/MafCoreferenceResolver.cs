using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

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

        private static readonly string[] Pronouns = ["它", "那个", "这个", "他", "她", "他们"];

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
                if (session.MessageHistory.Count > 0)
                {
                    // 简单策略：返回原文（完整实现需要更复杂的NLP）
                    _logger.LogDebug("Session has {Count} messages for coreference resolution", session.MessageHistory.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load session for coreference resolution");
            }

            return userInput;
        }
    }
}
