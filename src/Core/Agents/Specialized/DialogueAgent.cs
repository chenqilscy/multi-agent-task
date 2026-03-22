using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 对话 Agent
    /// 负责多轮对话管理和上下文维护
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 多轮对话：保持对话历史和上下文
    /// - 上下文管理：维护长期和短期记忆
    /// - 对话状态跟踪：追踪对话进度和状态
    /// - 自然对话：提供流畅的对话体验
    /// </remarks>
    public class DialogueAgent : MafBusinessAgentBase
    {
        public override string AgentId => "dialogue-agent-001";
        public override string Name => "DialogueAgent";
        public override string Description => "对话Agent，提供多轮对话和上下文管理";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "multi-turn-dialogue",
            "context-management",
            "conversation-state-tracking",
            "natural-conversation"
        };

        // 会话存储：SessionId -> 对话历史
        private readonly Dictionary<string, List<ChatMessage>> _sessionHistories;
        private readonly object _lock = new object();

        public DialogueAgent(
            IMafAiAgentRegistry llmRegistry,
            ILogger<DialogueAgent> logger)
            : base(llmRegistry, logger)
        {
            _sessionHistories = new Dictionary<string, List<ChatMessage>>();
        }

        /// <summary>
        /// 执行业务逻辑：多轮对话
        /// </summary>
        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var input = request.UserInput;
                if (string.IsNullOrWhiteSpace(input))
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Error = "输入不能为空"
                    };
                }

                // 获取会话 ID
                var sessionId = GetParameter(request, "sessionId", Guid.NewGuid().ToString());
                var systemPrompt = GetParameter(request, "systemPrompt", BuildDefaultSystemPrompt());
                var maxHistory = GetParameter(request, "maxHistory", 10);

                Logger.LogInformation("[Dialogue] 处理对话: {SessionId} - {Input}", sessionId, input);

                // 获取对话历史
                var history = GetOrCreateHistory(sessionId);

                // 添加用户消息
                var userMessage = new ChatMessage(ChatRole.User, input);
                history.Add(userMessage);

                // 调用 LLM（带历史记录）
                var response = await CallLlmChatAsync(
                    history.TakeLast(maxHistory),
                    LlmScenario.Chat,
                    ct);

                // 添加助手响应到历史
                var assistantMessage = new ChatMessage(ChatRole.Assistant, response);
                history.Add(assistantMessage);

                Logger.LogInformation("[Dialogue] 对话完成: {SessionId}", sessionId);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = response,
                    Data = new Dictionary<string, object>
                    {
                        ["session_id"] = sessionId,
                        ["response"] = response,
                        ["turn_count"] = history.Count,
                        ["last_user_message"] = input
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Dialogue] 对话处理失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"对话处理失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取或创建对话历史
        /// </summary>
        private List<ChatMessage> GetOrCreateHistory(string sessionId)
        {
            lock (_lock)
            {
                if (!_sessionHistories.TryGetValue(sessionId, out var history))
                {
                    history = new List<ChatMessage>();
                    _sessionHistories[sessionId] = history;
                }
                return history;
            }
        }

        /// <summary>
        /// 清除指定会话的历史
        /// </summary>
        public void ClearSession(string sessionId)
        {
            lock (_lock)
            {
                if (_sessionHistories.ContainsKey(sessionId))
                {
                    _sessionHistories.Remove(sessionId);
                    Logger.LogInformation("[Dialogue] 清除会话历史: {SessionId}", sessionId);
                }
            }
        }

        /// <summary>
        /// 获取会话历史
        /// </summary>
        public IReadOnlyList<ChatMessage> GetSessionHistory(string sessionId)
        {
            lock (_lock)
            {
                if (_sessionHistories.TryGetValue(sessionId, out var history))
                {
                    return history.ToList();
                }
                return Array.Empty<ChatMessage>();
            }
        }

        /// <summary>
        /// 构建默认系统提示词
        /// </summary>
        private string BuildDefaultSystemPrompt()
        {
            return "你是一个智能助手，擅长自然对话。请根据上下文提供友好、有帮助的回答。";
        }
    }
}
