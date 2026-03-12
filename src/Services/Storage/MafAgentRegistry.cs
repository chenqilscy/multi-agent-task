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

            await _cacheStore.SetAsync(
                $"{RegistryKeyPrefix}{registration.AgentId}",
                registration,
                TimeSpan.FromDays(1),
                ct);
        }

        /// <inheritdoc />
        public async Task UnregisterAsync(string agentId, CancellationToken ct = default)
        {
            _logger.LogInformation("Unregistering agent {AgentId}", agentId);
            await _cacheStore.DeleteAsync($"{RegistryKeyPrefix}{agentId}", ct);
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
            // 注意：这是简化实现，实际上应该有一个专门存储所有AgentId的键
            // 完整实现需要维护一个Agent ID列表
            _logger.LogDebug("Getting all registered agents");
            return new List<AgentRegistration>();
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
    }
}
