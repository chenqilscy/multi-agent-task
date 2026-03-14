using Microsoft.Agents.AI;
using Task = System.Threading.Tasks.Task;

namespace CKY.MultiAgentFramework.Core.Models.Session
{
    /// <summary>
    /// MAF Agent 会话管理类
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// 1. 继承 MS AF 的 AgentSession，充分利用框架能力
    /// 2. 扩展 MAF 特定的会话管理功能
    /// 3. 与 MafSessionState 配合使用，实现会话状态管理
    ///
    /// 架构关系：
    /// - MafAgentSession : AgentSession（MS AF 框架集成）
    ///   └─ 包含：MafSessionState（会话状态数据）
    ///
    /// 使用方式：
    /// - MS AF 框架会自动创建和管理 AgentSession
    /// - MafAiAgent 在 RunCoreAsync/RunCoreStreamingAsync 中使用此类
    /// - 自动集成 IMafAiSessionStore 实现持久化
    /// </remarks>
    public class MafAgentSession : AgentSession
    {
        /// <summary>
        /// MAF 会话状态数据（包含状态、Token 统计等）
        /// </summary>
        private MafSessionState? _mafSession;

        /// <summary>
        /// 会话存储（用于持久化）
        /// </summary>
        private readonly Core.Abstractions.IMafAiSessionStore? _sessionStore;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MafAgentSession(Core.Abstractions.IMafAiSessionStore? sessionStore = null)
        {
            _sessionStore = sessionStore;
        }

        /// <summary>
        /// 获取或创建 MAF 会话状态数据
        /// </summary>
        public MafSessionState MafSession
        {
            get
            {
                if (_mafSession == null)
                {
                    _mafSession = new MafSessionState
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    };
                }
                return _mafSession;
            }
        }

        /// <summary>
        /// 加载现有会话
        /// </summary>
        public async System.Threading.Tasks.Task<bool> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (_sessionStore == null)
                return false;

            var session = await _sessionStore.LoadAsync(sessionId, cancellationToken);
            if (session != null)
            {
                _mafSession = session;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 保存会话状态
        /// </summary>
        public async System.Threading.Tasks.Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (_sessionStore == null || _mafSession == null)
                return;

            await _sessionStore.SaveAsync(_mafSession, cancellationToken);
        }

        /// <summary>
        /// 更新活跃时间并增加 Token 使用量
        /// </summary>
        public void UpdateActivity(int tokensUsed = 0)
        {
            if (_mafSession != null)
            {
                _mafSession.UpdateActivity();
                if (tokensUsed > 0)
                {
                    _mafSession.AddTokens(tokensUsed);
                }
            }
        }

        /// <summary>
        /// 增加对话轮次
        /// </summary>
        public void IncrementTurn()
        {
            _mafSession?.IncrementTurn();
        }

        /// <summary>
        /// 检查会话是否已过期
        /// </summary>
        public bool IsExpired => _mafSession?.IsExpired ?? false;

        /// <summary>
        /// 检查会话是否活跃
        /// </summary>
        public bool IsActive => _mafSession?.IsActive ?? false;
    }
}
