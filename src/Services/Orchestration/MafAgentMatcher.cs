using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Orchestration
{
    /// <summary>
    /// Agent匹配服务
    /// 根据能力需求从注册表中查找最合适的Agent
    /// </summary>
    public class MafAgentMatcher : IAgentMatcher
    {
        private readonly IAgentRegistry _agentRegistry;
        private readonly ILogger<MafAgentMatcher> _logger;

        public MafAgentMatcher(
            IAgentRegistry agentRegistry,
            ILogger<MafAgentMatcher> logger)
        {
            _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string> FindBestAgentIdAsync(
            string requiredCapability,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Finding agent for capability: {Capability}", requiredCapability);

            var registration = await _agentRegistry.FindByCapabilityAsync(requiredCapability, ct);
            if (registration == null)
            {
                _logger.LogWarning("No agent found for capability: {Capability}", requiredCapability);
                throw new InvalidOperationException($"没有找到支持能力 '{requiredCapability}' 的Agent");
            }

            return registration.AgentId;
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, string>> MatchBatchAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default)
        {
            var result = new Dictionary<string, string>();

            foreach (var task in tasks)
            {
                try
                {
                    var agentId = await FindBestAgentIdAsync(task.RequiredCapability, ct);
                    result[task.TaskId] = agentId;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to match agent for task {TaskId}", task.TaskId);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetAvailableAgentIdsAsync(CancellationToken ct = default)
        {
            var agents = await _agentRegistry.GetAllAsync(ct);
            return agents.Select(a => a.AgentId).ToList();
        }
    }
}
