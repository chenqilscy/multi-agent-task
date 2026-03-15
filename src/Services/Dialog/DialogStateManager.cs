using CKY.MultiAgentFramework.Core.Models.Dialog;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 对话状态管理器
    /// 管理对话状态栈，支持多话题切换和回退
    /// Dialog state manager - manages dialog state stack with topic switching and rollback
    /// </summary>
    public class DialogStateManager
    {
        private readonly Stack<DialogState> _stateStack;
        private readonly ILogger<DialogStateManager> _logger;
        private readonly object _lock = new object();

        public DialogStateManager(ILogger<DialogStateManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateStack = new Stack<DialogState>();
        }

        /// <summary>
        /// 获取当前状态栈深度
        /// </summary>
        public int StackDepth
        {
            get
            {
                lock (_lock)
                {
                    return _stateStack.Count;
                }
            }
        }

        /// <summary>
        /// 推入新状态
        /// </summary>
        /// <param name="state">对话状态</param>
        /// <param name="ct">取消令牌</param>
        public Task PushStateAsync(DialogState state, CancellationToken ct = default)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            lock (_lock)
            {
                _stateStack.Push(state);
                _logger.LogDebug("Pushed dialog state. Intent: {Intent}, Stack depth: {Depth}",
                    state.CurrentIntent, _stateStack.Count);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 弹出当前状态
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>弹出的状态，如果栈为空则返回null</returns>
        public Task<DialogState?> PopStateAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_stateStack.Count == 0)
                {
                    _logger.LogDebug("Cannot pop state: stack is empty");
                    return Task.FromResult<DialogState?>(null);
                }

                var state = _stateStack.Pop();
                _logger.LogDebug("Popped dialog state. Intent: {Intent}, Stack depth: {Depth}",
                    state.CurrentIntent, _stateStack.Count);

                return Task.FromResult<DialogState?>(state);
            }
        }

        /// <summary>
        /// 获取当前状态（不弹出）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>当前状态，如果栈为空则返回null</returns>
        public Task<DialogState?> GetCurrentStateAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_stateStack.Count == 0)
                {
                    return Task.FromResult<DialogState?>(null);
                }

                var state = _stateStack.Peek();
                return Task.FromResult<DialogState?>(state);
            }
        }

        /// <summary>
        /// 处理话题切换
        /// </summary>
        /// <param name="newIntent">新意图</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>是否需要保存当前状态</returns>
        public async Task<bool> HandleTopicSwitchAsync(string newIntent, CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_stateStack.Count == 0)
                {
                    _logger.LogDebug("No current state, no need to save for topic switch");
                    return false;
                }

                var currentState = _stateStack.Peek();

                // 如果新意图与当前意图不同，需要保存当前状态
                if (currentState.CurrentIntent != newIntent)
                {
                    _logger.LogInformation("Topic switch detected. Saving current state: {CurrentIntent} -> {NewIntent}",
                        currentState.CurrentIntent, newIntent);
                    return true;
                }

                _logger.LogDebug("No topic switch needed: intent remains {Intent}", newIntent);
                return false;
            }
        }

        /// <summary>
        /// 回退到上一个状态
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>是否成功回退</returns>
        public async Task<bool> RollbackAsync(CancellationToken ct = default)
        {
            var poppedState = await PopStateAsync(ct);

            if (poppedState != null)
            {
                _logger.LogInformation("Rolled back from state: {Intent}", poppedState.CurrentIntent);
                return true;
            }

            _logger.LogDebug("Cannot rollback: no previous state");
            return false;
        }

        /// <summary>
        /// 清空所有状态
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public Task ClearAllAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                var count = _stateStack.Count;
                _stateStack.Clear();
                _logger.LogInformation("Cleared all dialog states. Count: {Count}", count);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取所有状态（不修改栈）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>状态列表（从栈顶到栈底）</returns>
        public Task<IReadOnlyList<DialogState>> GetAllStatesAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult<IReadOnlyList<DialogState>>(_stateStack.ToList());
            }
        }
    }

    /// <summary>
    /// 对话状态
    /// </summary>
    public class DialogState
    {
        /// <summary>
        /// 当前意图
        /// </summary>
        public string CurrentIntent { get; set; } = string.Empty;

        /// <summary>
        /// 槽位值
        /// </summary>
        public Dictionary<string, object> SlotValues { get; set; } = new();

        /// <summary>
        /// 状态创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 用户输入历史（在这个状态下）
        /// </summary>
        public List<string> UserInputs { get; set; } = new();

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
