using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 对话状态管理器实现
    /// 管理对话上下文、轮次追踪和历史槽位
    /// Dialog state manager implementation - manages dialog context, turn tracking, and historical slots
    /// </summary>
    public class DialogStateManager : IDialogStateManager
    {
        private readonly IMafSessionStorage _sessionStorage;
        private readonly ILogger<DialogStateManager> _logger;

        private const string DialogContextKey = "dialog_context";

        /// <summary>
        /// 初始化 DialogStateManager
        /// Initialize DialogStateManager
        /// </summary>
        /// <param name="sessionStorage">会话存储</param>
        /// <param name="logger">日志记录器</param>
        /// <exception cref="ArgumentNullException">参数为 null 时抛出</exception>
        public DialogStateManager(
            IMafSessionStorage sessionStorage,
            ILogger<DialogStateManager> logger)
        {
            _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<DialogContext> LoadOrCreateAsync(
            string conversationId,
            string userId,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Loading or creating dialog context for conversation {ConversationId} / 正在加载或创建对话上下文", conversationId);

            // 尝试从会话存储加载
            // Try to load from session storage
            IAgentSession? session = null;
            try
            {
                if (await _sessionStorage.ExistsAsync(conversationId, ct))
                {
                    session = await _sessionStorage.LoadSessionAsync(conversationId, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load session from storage, creating new context / 从存储加载会话失败，创建新上下文");
            }

            // 检查是否存在 DialogContext
            // Check if DialogContext exists
            if (session != null &&
                session.Context.TryGetValue(DialogContextKey, out var contextObj) &&
                contextObj is DialogContext context)
            {
                _logger.LogDebug("Loaded existing dialog context: TurnCount={TurnCount} / 已加载现有对话上下文", context.TurnCount);
                return context;
            }

            // 创建新的 DialogContext
            // Create new DialogContext
            var newContext = new DialogContext
            {
                SessionId = conversationId,
                UserId = userId,
                TurnCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Created new dialog context for conversation {ConversationId} / 已为对话创建新上下文", conversationId);

            return newContext;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> slots,
            List<TaskExecutionResult> executionResults,
            CancellationToken ct = default)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger.LogDebug("Updating dialog context: TurnCount={TurnCount}, Intent={Intent} / 正在更新对话上下文",
                context.TurnCount, intent);

            // 更新 TurnCount 和 PreviousIntent
            // Update TurnCount and PreviousIntent
            context.TurnCount++;
            context.PreviousIntent = intent;
            context.PreviousSlots = new Dictionary<string, object>(slots);
            context.UpdatedAt = DateTime.UtcNow;

            // 更新 HistoricalSlots（记录槽位值的出现频次）
            // Update HistoricalSlots (record slot value frequency)
            foreach (var slot in slots)
            {
                var key = $"{intent}.{slot.Key}";
                if (context.HistoricalSlots.ContainsKey(key))
                {
                    var count = (int)context.HistoricalSlots[key];
                    context.HistoricalSlots[key] = count + 1;
                }
                else
                {
                    context.HistoricalSlots[key] = 1;
                }
            }

            // 保存到会话存储
            // Save to session storage
            try
            {
                IAgentSession? session = null;
                try
                {
                    session = await _sessionStorage.LoadSessionAsync(context.SessionId, ct);
                }
                catch
                {
                    // 会话不存在，忽略
                }

                if (session != null)
                {
                    session.Context[DialogContextKey] = context;
                    await _sessionStorage.SaveSessionAsync(session, ct);
                }

                _logger.LogDebug("Dialog context updated: TurnCount={TurnCount}, HistoricalSlots={SlotCount} / 对话上下文已更新",
                    context.TurnCount, context.HistoricalSlots.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save dialog context to storage / 保存对话上下文到存储失败");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RecordPendingClarificationAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> detectedSlots,
            List<SlotDefinition> missingSlots,
            CancellationToken ct = default)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (detectedSlots == null)
            {
                throw new ArgumentNullException(nameof(detectedSlots));
            }

            if (missingSlots == null)
            {
                throw new ArgumentNullException(nameof(missingSlots));
            }

            _logger.LogInformation("Recording pending clarification for intent {Intent} / 正在记录待处理的澄清", intent);

            context.PendingClarification = new PendingClarificationInfo
            {
                Intent = intent,
                DetectedSlots = new Dictionary<string, object>(detectedSlots),
                MissingSlots = new List<SlotDefinition>(missingSlots),
                CreatedAt = DateTime.UtcNow
            };
            context.UpdatedAt = DateTime.UtcNow;

            // 保存到会话存储
            // Save to session storage
            await SaveContextAsync(context, ct);

            _logger.LogDebug("Pending clarification recorded: {Count} missing slots / 已记录待处理的澄清", missingSlots.Count);
        }

        /// <inheritdoc />
        public async Task RecordPendingTasksAsync(
            DialogContext context,
            ExecutionPlan plan,
            Dictionary<string, object> filledSlots,
            CancellationToken ct = default)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (filledSlots == null)
            {
                throw new ArgumentNullException(nameof(filledSlots));
            }

            // 获取仍然缺失的槽位
            // Get still missing slots
            var stillMissing = new List<SlotDefinition>();

            _logger.LogInformation("Recording pending tasks / 正在记录待处理的任务");

            context.PendingTask = new PendingTaskInfo
            {
                Plan = plan,
                FilledSlots = new Dictionary<string, object>(filledSlots),
                StillMissing = stillMissing,
                CreatedAt = DateTime.UtcNow
            };
            context.UpdatedAt = DateTime.UtcNow;

            // 保存到会话存储
            // Save to session storage
            await SaveContextAsync(context, ct);

            _logger.LogDebug("Pending task recorded: {Count} filled slots / 已记录待处理的任务", filledSlots.Count);
        }

        /// <inheritdoc />
        public async Task<MafTaskResponse> HandleClarificationResponseAsync(
            string conversationId,
            string userResponse,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty / 对话 ID 不能为空", nameof(conversationId));
            }

            if (string.IsNullOrEmpty(userResponse))
            {
                throw new ArgumentException("User response cannot be null or empty / 用户响应不能为空", nameof(userResponse));
            }

            _logger.LogInformation("Handling clarification response for conversation {ConversationId} / 正在处理对话的澄清响应",
                conversationId);

            var context = await LoadOrCreateAsync(conversationId, "", ct);

            if (context.PendingClarification == null)
            {
                _logger.LogWarning("No pending clarification found for conversation {ConversationId} / 未找到待处理的澄清", conversationId);
                return new MafTaskResponse
                {
                    Success = false,
                    Result = "没有待处理的澄清问题 / No pending clarification questions"
                };
            }

            // 解析用户响应并填充槽位（需调用 IEntityExtractor 或 ILlmService 完成实体提取）
            // Parse user response and fill slots (requires IEntityExtractor or ILlmService for entity extraction)

            _logger.LogInformation("Clarification response processed for intent {Intent} / 已处理意图的澄清响应",
                context.PendingClarification.Intent);

            // 清除待处理的澄清
            // Clear pending clarification
            context.PendingClarification = null;
            context.UpdatedAt = DateTime.UtcNow;

            await SaveContextAsync(context, ct);

            return new MafTaskResponse
            {
                Success = true,
                Result = "已理解您的响应 / Understood your response"
            };
        }

        /// <summary>
        /// 保存对话上下文到会话存储
        /// Save dialog context to session storage
        /// </summary>
        /// <param name="context">对话上下文</param>
        /// <param name="ct">取消令牌</param>
        private async Task SaveContextAsync(DialogContext context, CancellationToken ct)
        {
            try
            {
                IAgentSession? session = null;
                try
                {
                    session = await _sessionStorage.LoadSessionAsync(context.SessionId, ct);
                }
                catch
                {
                    // 会话不存在，忽略
                }

                if (session != null)
                {
                    session.Context[DialogContextKey] = context;
                    await _sessionStorage.SaveSessionAsync(session, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save dialog context / 保存对话上下文失败");
                throw;
            }
        }
    }
}
