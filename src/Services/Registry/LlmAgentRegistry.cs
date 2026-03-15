using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Registry
{
    /// <summary>
    /// LLM Agent 注册表实现
    /// </summary>
    public class MafAiAgentRegistry : IMafAiAgentRegistry
    {
        private readonly ILogger<MafAiAgentRegistry> _logger;
        private readonly List<MafAiAgent> _agents = new();
        private readonly object _lock = new();

        public MafAiAgentRegistry(ILogger<MafAiAgentRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RegisterAgent(MafAiAgent agent)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));

            lock (_lock)
            {
                // 检查是否已注册
                if (_agents.Any(a => a.AgentId == agent.AgentId))
                {
                    _logger.LogWarning("[Registry] Agent {AgentId} already registered, skipping", agent.AgentId);
                    return;
                }

                _agents.Add(agent);
                _logger.LogInformation("[Registry] Registered agent: {AgentId}", agent.AgentId);
            }
        }

        public void RegisterAgents(IEnumerable<MafAiAgent> agents)
        {
            if (agents == null) throw new ArgumentNullException(nameof(agents));

            foreach (var agent in agents)
            {
                RegisterAgent(agent);
            }
        }

        public async Task<MafAiAgent> GetBestAgentAsync(
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            await Task.Yield();

            lock (_lock)
            {
                // 查找支持该场景的已启用 Agent
                var candidates = _agents
                    .Where(a => a.SupportsScenario(scenario))
                    .OrderBy(a => a.Config.Priority)
                    .ToList();

                if (candidates.Count == 0)
                {
                    _logger.LogError("[Registry] No agent available for scenario: {Scenario}", scenario);
                    throw new InvalidOperationException($"No agent available for scenario: {scenario}");
                }

                // 返回优先级最高的
                var bestAgent = candidates[0];
                _logger.LogDebug("[Registry] Selected agent {AgentId} for scenario {Scenario}",
                    bestAgent.AgentId, scenario);

                return bestAgent;
            }
        }

        public Task<MafAiAgent?> GetAgentByProviderAsync(
            string providerName,
            CancellationToken ct = default)
        {
            Task.Yield();

            lock (_lock)
            {
                var agent = _agents.FirstOrDefault(a => a.Config.ProviderName == providerName);
                return Task.FromResult(agent);
            }
        }

        public IReadOnlyList<MafAiAgent> GetAllAgents()
        {
            lock (_lock)
            {
                return _agents.ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<MafAiAgent> GetAgentsByScenario(LlmScenario scenario)
        {
            lock (_lock)
            {
                return _agents
                    .Where(a => a.SupportsScenario(scenario))
                    .ToList()
                    .AsReadOnly();
            }
        }

        public Task SetAgentEnabledAsync(string providerName, bool enabled, CancellationToken ct = default)
        {
            // 注意：由于 Config 是对象引用，这里只是标记，实际禁用需要在 GetBestAgentAsync 中检查
            _logger.LogInformation("[Registry] Agent {ProviderName} enable set to: {Enabled}",
                providerName, enabled);

            // 查找并更新 IsEnabled 标志
            lock (_lock)
            {
                var agent = _agents.FirstOrDefault(a => a.Config.ProviderName == providerName);
                if (agent != null)
                {
                    agent.Config.IsEnabled = enabled;
                    _logger.LogInformation("[Registry] Agent {ProviderName} IsEnabled updated to {Enabled}",
                        providerName, enabled);
                }
            }

            return Task.CompletedTask;
        }

        public Task ReloadFromDatabaseAsync(CancellationToken ct = default)
        {
            // 从数据库重新加载配置（当前使用内存中的配置，数据库集成在 Task 11 中实现）
            _logger.LogInformation("[Registry] ReloadFromDatabaseAsync called. Using in-memory configuration (database integration pending).");
            return Task.CompletedTask;
        }
    }
}
