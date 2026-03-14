using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Orchestration
{
    /// <summary>
    /// LLM Agent 注册表实现
    /// 负责管理多个 LLM Agent 实例，根据场景和优先级动态选择
    /// </summary>
    public class LlmAgentRegistry : ILlmAgentRegistry
    {
        private readonly Dictionary<string, LlmAgent> _agentsByKey = new();
        private readonly Dictionary<LlmScenario, List<LlmAgent>> _agentsByScenario = new();
        private readonly ILogger<LlmAgentRegistry> _logger;
        private readonly object _lock = new();

        public LlmAgentRegistry(ILogger<LlmAgentRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化场景字典
            foreach (LlmScenario scenario in Enum.GetValues<LlmScenario>())
            {
                _agentsByScenario[scenario] = new List<LlmAgent>();
            }
        }

        /// <inheritdoc />
        public void RegisterAgent(LlmAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            var key = GetAgentKey(agent.Config.ProviderName, agent.Config.ModelId);

            lock (_lock)
            {
                if (_agentsByKey.ContainsKey(key))
                {
                    _logger.LogWarning("LLM Agent {ProviderName}:{ModelId} is already registered, skipping",
                        agent.Config.ProviderName, agent.Config.ModelId);
                    return;
                }

                _agentsByKey[key] = agent;

                // 按场景索引
                foreach (var scenario in agent.Config.SupportedScenarios)
                {
                    _agentsByScenario[scenario].Add(agent);
                }

                _logger.LogInformation("Registered LLM Agent: {ProviderName}:{ModelId} with {ScenarioCount} scenarios",
                    agent.Config.ProviderName, agent.Config.ModelId, agent.Config.SupportedScenarios.Count);
            }
        }

        /// <inheritdoc />
        public void RegisterAgents(IEnumerable<LlmAgent> agents)
        {
            if (agents == null)
                throw new ArgumentNullException(nameof(agents));

            foreach (var agent in agents)
            {
                RegisterAgent(agent);
            }
        }

        /// <inheritdoc />
        public async Task<LlmAgent> GetBestAgentAsync(LlmScenario scenario, CancellationToken ct = default)
        {
            var agents = GetAgentsByScenario(scenario);

            if (agents.Count == 0)
                throw new InvalidOperationException($"No LLM agent available for scenario: {scenario}");

            // 按优先级排序（Priority 值越小优先级越高）
            var availableAgents = agents
                .Where(a => a.Config.IsEnabled)
                .OrderBy(a => a.Config.Priority)
                .ToList();

            if (availableAgents.Count == 0)
                throw new InvalidOperationException($"No enabled LLM agent available for scenario: {scenario}");

            // 返回优先级最高的可用 agent
            var bestAgent = availableAgents.First();
            _logger.LogDebug("Selected LLM Agent {ProviderName}:{ModelId} for scenario {Scenario}",
                bestAgent.Config.ProviderName, bestAgent.Config.ModelId, scenario);

            return await Task.FromResult(bestAgent);
        }

        /// <inheritdoc />
        public Task<LlmAgent?> GetAgentByProviderAsync(string providerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));

            lock (_lock)
            {
                var agent = _agentsByKey.Values
                    .FirstOrDefault(a => a.Config.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

                return Task.FromResult(agent);
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<LlmAgent> GetAllAgents()
        {
            lock (_lock)
            {
                return _agentsByKey.Values.ToList().AsReadOnly();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<LlmAgent> GetAgentsByScenario(LlmScenario scenario)
        {
            lock (_lock)
            {
                return _agentsByScenario.GetValueOrDefault(scenario, new List<LlmAgent>()).ToList().AsReadOnly();
            }
        }

        /// <inheritdoc />
        public Task SetAgentEnabledAsync(string providerName, bool enabled, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));

            lock (_lock)
            {
                var agent = _agentsByKey.Values
                    .FirstOrDefault(a => a.Config.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

                if (agent == null)
                {
                    _logger.LogWarning("LLM Agent {ProviderName} not found, cannot set enabled to {Enabled}",
                        providerName, enabled);
                    return Task.CompletedTask;
                }

                agent.Config.IsEnabled = enabled;
                _logger.LogInformation("LLM Agent {ProviderName} enabled set to {Enabled}", providerName, enabled);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ReloadFromDatabaseAsync(CancellationToken ct = default)
        {
            // TODO: 实现从数据库加载配置
            // 1. 从数据库读取 LlmProviderConfig
            // 2. 根据配置创建对应的 LlmAgent 实例
            // 3. 清空当前注册表并重新注册

            _logger.LogInformation("Reloading LLM agents from database (not yet implemented)");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 生成 Agent 唯一键
        /// </summary>
        private static string GetAgentKey(string providerName, string modelId)
        {
            return $"{providerName.ToLowerInvariant()}:{modelId.ToLowerInvariant()}";
        }
    }
}
