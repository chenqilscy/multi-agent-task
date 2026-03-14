using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Storage
{
    /// <summary>
    /// Agent注册表服务
    /// 管理系统中所有Agent的注册信息
    /// </summary>
    public class MafAgentRegistry : IAgentRegistry
    {
        private readonly ICacheStore _cacheStore;
        private readonly ILogger<MafAgentRegistry> _logger;

        private const string RegistryKeyPrefix = "maf:agent:";
        private const string AllAgentIdsKey = "maf:agent:_all_ids";

        public MafAgentRegistry(
            ICacheStore cacheStore,
            ILogger<MafAgentRegistry> logger)
        {
            _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task RegisterAsync(
            AgentRegistration registration,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Registering agent {AgentId}: {AgentName}", registration.AgentId, registration.Name);
            registration.RegisteredAt = DateTime.UtcNow;
            registration.LastHeartbeat = DateTime.UtcNow;

            // 存储Agent注册信息
            await _cacheStore.SetAsync(
                $"{RegistryKeyPrefix}{registration.AgentId}",
                registration,
                TimeSpan.FromDays(1),
                ct);

            // 将AgentId添加到全局集合中
            await AddAgentIdToAllIdsAsync(registration.AgentId, ct);
        }

        /// <inheritdoc />
        public async Task UnregisterAsync(string agentId, CancellationToken ct = default)
        {
            _logger.LogInformation("Unregistering agent {AgentId}", agentId);

            // 删除Agent注册信息
            await _cacheStore.DeleteAsync($"{RegistryKeyPrefix}{agentId}", ct);

            // 从全局集合中移除AgentId
            await RemoveAgentIdFromAllIdsAsync(agentId, ct);
        }

        /// <inheritdoc />
        public async Task<AgentRegistration?> FindByCapabilityAsync(
            string capability,
            CancellationToken ct = default)
        {
            var all = await GetAllAsync(ct);
            return all.FirstOrDefault(a =>
                a.Status == MafAgentStatus.Idle &&
                a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase));
        }

        /// <inheritdoc />
        public async Task<AgentRegistration?> FindByIdAsync(
            string agentId,
            CancellationToken ct = default)
        {
            return await _cacheStore.GetAsync<AgentRegistration>(
                $"{RegistryKeyPrefix}{agentId}",
                ct);
        }

        /// <inheritdoc />
        public async Task<List<AgentRegistration>> GetAllAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Getting all registered agents");

            // 获取所有AgentId
            var allIds = await GetAllAgentIdsAsync(ct);

            // 批量获取Agent注册信息
            var agents = new List<AgentRegistration>();
            foreach (var agentId in allIds)
            {
                var registration = await FindByIdAsync(agentId, ct);
                if (registration != null)
                {
                    agents.Add(registration);
                }
            }

            _logger.LogDebug("Found {Count} registered agents", agents.Count);
            return agents;
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(
            string agentId,
            MafAgentStatus status,
            CancellationToken ct = default)
        {
            var registration = await FindByIdAsync(agentId, ct);
            if (registration != null)
            {
                registration.Status = status;
                registration.LastHeartbeat = DateTime.UtcNow;
                await _cacheStore.SetAsync(
                    $"{RegistryKeyPrefix}{agentId}",
                    registration,
                    TimeSpan.FromDays(1),
                    ct);
            }
        }

        /// <summary>
        /// 获取所有AgentId列表
        /// </summary>
        private async Task<List<string>> GetAllAgentIdsAsync(CancellationToken ct)
        {
            var result = await _cacheStore.GetAsync<List<string>>(AllAgentIdsKey, ct);
            return result ?? new List<string>();
        }

        /// <summary>
        /// 添加AgentId到全局集合
        /// </summary>
        private async Task AddAgentIdToAllIdsAsync(string agentId, CancellationToken ct)
        {
            var allIds = await GetAllAgentIdsAsync(ct);

            if (!allIds.Contains(agentId, StringComparer.OrdinalIgnoreCase))
            {
                allIds.Add(agentId);
                await _cacheStore.SetAsync(
                    AllAgentIdsKey,
                    allIds,
                    TimeSpan.FromDays(7), // AgentId列表保留7天
                    ct);
                _logger.LogDebug("Added AgentId {AgentId} to all-ids collection", agentId);
            }
        }

        /// <summary>
        /// 从全局集合中移除AgentId
        /// </summary>
        private async Task RemoveAgentIdFromAllIdsAsync(string agentId, CancellationToken ct)
        {
            var allIds = await GetAllAgentIdsAsync(ct);

            var removed = allIds.RemoveAll(id => string.Equals(id, agentId, StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                await _cacheStore.SetAsync(
                    AllAgentIdsKey,
                    allIds,
                    TimeSpan.FromDays(7),
                    ct);
                _logger.LogDebug("Removed AgentId {AgentId} from all-ids collection", agentId);
            }
        }
    }
}
