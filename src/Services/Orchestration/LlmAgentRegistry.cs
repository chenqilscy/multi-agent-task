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
    public class MafAiAgentRegistry : IMafAiAgentRegistry
    {
        private readonly Dictionary<string, MafAiAgent> _agentsByKey = new();
        private readonly Dictionary<LlmScenario, List<MafAiAgent>> _agentsByScenario = new();
        private readonly ILogger<MafAiAgentRegistry> _logger;
        private readonly object _lock = new();

        public MafAiAgentRegistry(ILogger<MafAiAgentRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化场景字典
            foreach (LlmScenario scenario in Enum.GetValues<LlmScenario>())
            {
                _agentsByScenario[scenario] = new List<MafAiAgent>();
            }
        }

        /// <inheritdoc />
        public void RegisterAgent(MafAiAgent agent)
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
        public void RegisterAgents(IEnumerable<MafAiAgent> agents)
        {
            if (agents == null)
                throw new ArgumentNullException(nameof(agents));

            foreach (var agent in agents)
            {
                RegisterAgent(agent);
            }
        }

        /// <inheritdoc />
        public async Task<MafAiAgent> GetBestAgentAsync(LlmScenario scenario, CancellationToken ct = default)
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
        public Task<MafAiAgent?> GetAgentByProviderAsync(string providerName, CancellationToken ct = default)
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
        public IReadOnlyList<MafAiAgent> GetAllAgents()
        {
            lock (_lock)
            {
                return _agentsByKey.Values.ToList().AsReadOnly();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<MafAiAgent> GetAgentsByScenario(LlmScenario scenario)
        {
            lock (_lock)
            {
                return _agentsByScenario.GetValueOrDefault(scenario, new List<MafAiAgent>()).ToList().AsReadOnly();
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
            // 从数据库重新加载配置步骤：
            // 1. 从数据库读取 LlmProviderConfig
            // 2. 根据配置创建对应的 LlmAgent 实例
            // 3. 清空当前注册表并重新注册
            // 当前使用内存中的配置（数据库集成在后续版本中实现）

            _logger.LogInformation("ReloadFromDatabaseAsync called. Using in-memory configuration (database integration pending).");

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
