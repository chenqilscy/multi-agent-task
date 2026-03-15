using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Session;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Repository.Repositories
{
    /// <summary>
    /// 基于 EF Core 数据库的 MafAiAgent 会话存储实现
    /// </summary>
    /// <remarks>
    /// 特性：
    /// - 持久化存储（支持 SQLite、PostgreSQL 等）
    /// - 事务支持
    /// - 异步操作
    /// - LINQ 查询支持
    ///
    /// 使用场景：
    /// - 长期会话存档（超过 Redis TTL 的会话）
    /// - 会话分析和审计
    /// - 跨实例会话恢复
    /// </remarks>
    public class DatabaseMafAiSessionStore : IMafAiSessionStore
    {
        private readonly ILogger<DatabaseMafAiSessionStore> _logger;
        private readonly Func<MafDbContext> _dbContextFactory;
        private readonly DatabaseSessionOptions _options;

        public DatabaseMafAiSessionStore(
            Func<MafDbContext> dbContextFactory,
            ILogger<DatabaseMafAiSessionStore> logger,
            DatabaseSessionOptions? options = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new DatabaseSessionOptions();
        }

        public async Task SaveAsync(MafSessionState session, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory();

                var existingEntity = await context.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == session.SessionId, cancellationToken);

                if (existingEntity != null)
                {
                    // 更新现有记录
                    context.Entry(existingEntity).CurrentValues.SetValues(session);
                }
                else
                {
                    // 插入新记录
                    context.Sessions.Add(session);
                }

                await context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("[DatabaseSession] Saved session: {SessionId}", session.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to save session: {SessionId}", session.SessionId);
                throw;
            }
        }

        public async Task<MafSessionState?> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory();

                var session = await context.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

                if (session == null)
                {
                    return null;
                }

                // 检查是否过期
                if (session.IsExpired)
                {
                    await DeleteAsync(sessionId, cancellationToken);
                    return null;
                }

                _logger.LogDebug("[DatabaseSession] Loaded session: {SessionId}", sessionId);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to load session: {SessionId}", sessionId);
                return null;
            }
        }

        public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory();

                var entity = await context.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

                if (entity != null)
                {
                    context.Sessions.Remove(entity);
                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogDebug("[DatabaseSession] Deleted session: {SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to delete session: {SessionId}", sessionId);
            }
        }

        public async Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory();
                return await context.Sessions
                    .AnyAsync(s => s.SessionId == sessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to check session existence: {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<List<MafSessionState>> GetSessionsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory();

                var entities = await context.Sessions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.LastActivityAt)
                    .ToListAsync(cancellationToken);

                return entities
                    .Where(s => s != null && !s.IsExpired)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to get sessions for user: {UserId}", userId);
                return new List<MafSessionState>();
            }
        }

        public async Task<List<MafSessionState>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory();

                var entities = await context.Sessions
                    .Where(s => s.Status == (int)SessionStatus.Active)
                    .Where(s => !s.ExpiresAt.HasValue || s.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(s => s.LastActivityAt)
                    .Take(_options.MaxQueryResults)
                    .ToListAsync(cancellationToken);

                return entities
                    .Where(s => s != null && s.IsActive)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to get active sessions");
                return new List<MafSessionState>();
            }
        }

        public async Task<int> DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory();

                var expiredEntities = await context.Sessions
                    .Where(s => s.ExpiresAt.HasValue && s.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync(cancellationToken);

                if (expiredEntities.Any())
                {
                    context.Sessions.RemoveRange(expiredEntities);
                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("[DatabaseSession] Deleted {Count} expired sessions", expiredEntities.Count);
                }

                return expiredEntities.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to delete expired sessions");
                return 0;
            }
        }

        public async Task SaveBatchAsync(IEnumerable<MafSessionState> sessions, CancellationToken cancellationToken = default)
        {
            var sessionList = sessions.ToList();
            if (sessionList.Count == 0) return;

            try
            {
                using var context = _dbContextFactory();
                using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    foreach (var session in sessionList)
                    {
                        var existing = await context.Sessions
                            .FirstOrDefaultAsync(s => s.SessionId == session.SessionId, cancellationToken);

                        if (existing != null)
                        {
                            context.Entry(existing).CurrentValues.SetValues(session);
                        }
                        else
                        {
                            context.Sessions.Add(session);
                        }
                    }

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation("[DatabaseSession] Saved {Count} sessions in batch", sessionList.Count);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to save batch sessions");
                throw;
            }
        }

        public async Task<List<MafSessionState>> LoadBatchAsync(IEnumerable<string> sessionIds, CancellationToken cancellationToken = default)
        {
            var sessionIdList = sessionIds.ToList();
            if (sessionIdList.Count == 0) return new List<MafSessionState>();

            try
            {
                using var context = _dbContextFactory();

                var entities = await context.Sessions
                    .Where(s => sessionIdList.Contains(s.SessionId))
                    .ToListAsync(cancellationToken);

                var sessions = entities.ToDictionary(e => e.SessionId, e => e);

                var result = new List<MafSessionState>();
                foreach (var sessionId in sessionIdList)
                {
                    if (sessions.TryGetValue(sessionId, out var session) && session != null && !session.IsExpired)
                    {
                        result.Add(session);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSession] Failed to load batch sessions");
                return new List<MafSessionState>();
            }
        }
    }

    /// <summary>
    /// 数据库会话存储配置选项
    /// </summary>
    public class DatabaseSessionOptions
    {
        /// <summary>
        /// 最大查询结果数量（防止内存溢出）
        /// </summary>
        public int MaxQueryResults { get; set; } = 1000;

        /// <summary>
        /// 批量保存的最大批次大小
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

        /// <summary>
        /// 是否启用软删除（标记删除而非物理删除）
        /// </summary>
        public bool EnableSoftDelete { get; set; } = false;

        /// <summary>
        /// 软删除的保留天数（超过此天数后物理删除）
        /// </summary>
        public int SoftDeleteRetentionDays { get; set; } = 30;
    }
}
