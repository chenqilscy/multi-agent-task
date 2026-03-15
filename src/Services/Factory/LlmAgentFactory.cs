using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Agents.Providers;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace CKY.MultiAgentFramework.Services.Factory
{
    /// <summary>
    /// LLM Agent 工厂实现
    /// 根据提供商配置和场景动态创建 LLM Agent 实例
    /// </summary>
    /// <remarks>
    /// 设计模式：
    /// 1. 工厂模式 - 根据配置创建 Agent
    /// 2. 策略模式 - 不同提供商有不同的创建策略
    /// 3. 依赖注入 - 使用 ILogger 和配置仓储
    ///
    /// 扩展性：
    /// 添加新的提供商时，只需在 CreateAgentAsync 中添加新的 case 分支
    /// </remarks>
    public class LlmAgentFactory : ILlmAgentFactory
    {
        private readonly ILogger<LlmAgentFactory> _logger;
        private readonly ILlmProviderConfigRepository _configRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory? _httpClientFactory;

        public LlmAgentFactory(
            ILogger<LlmAgentFactory> logger,
            ILlmProviderConfigRepository configRepository,
            ILoggerFactory? loggerFactory = null,
            IHttpClientFactory? httpClientFactory = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            _httpClientFactory = httpClientFactory;
        }

        #region 核心创建方法

        /// <summary>
        /// 根据提供商配置创建 LLM Agent（核心方法）
        /// </summary>
        public async Task<MafAiAgent> CreateAgentAsync(
            LlmProviderConfig config,
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // 验证配置
            config.Validate();

            // 检查是否支持该场景
            if (!config.SupportedScenarios.Contains(scenario))
            {
                throw new InvalidOperationException(
                    $"Provider {config.ProviderName} does not support scenario {scenario}. " +
                    $"Supported scenarios: {string.Join(", ", config.SupportedScenarios)}");
            }

            // 检查是否启用
            if (!config.IsEnabled)
            {
                throw new InvalidOperationException(
                    $"Provider {config.ProviderName} is disabled.");
            }

            _logger.LogInformation(
                "[Factory] Creating LLM Agent: {ProviderName} - {ModelId} for scenario {Scenario}",
                config.ProviderName,
                config.ModelId,
                scenario);

            // 根据提供商名称创建对应的 Agent 实例
            MafAiAgent agent = config.ProviderName.ToLowerInvariant() switch
            {
                "zhipuai" => await CreateZhipuAIAgentAsync(config, ct),
                "tongyi" => await CreateTongyiAgentAsync(config, ct),
                "qwen" => await CreateQwenAgentAsync(config, ct),
                "wenxin" => await CreateWenxinAgentAsync(config, ct),
                "xunfei" => await CreateXunfeiAgentAsync(config, ct),
                "baichuan" => await CreateBaichuanAgentAsync(config, ct),
                "minimax" => await CreateMiniMaxAgentAsync(config, ct),
                _ => throw new NotSupportedException(
                    $"Provider {config.ProviderName} is not supported. " +
                    $"Supported providers: zhipuai, tongyi, qwen, wenxin, xunfei, baichuan, minimax")
            };

            _logger.LogInformation(
                "[Factory] Successfully created agent: {AgentId}",
                agent.AgentId);

            return agent;
        }

        /// <summary>
        /// 根据提供商名称创建 LLM Agent（从数据库加载配置）
        /// </summary>
        public async Task<MafAiAgent> CreateAgentByProviderAsync(
            string providerName,
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name cannot be empty", nameof(providerName));

            _logger.LogDebug(
                "[Factory] Loading config for provider: {ProviderName}",
                providerName);

            // 从数据库加载配置
            var config = await _configRepository.GetByNameAsync(providerName, ct);

            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Provider {providerName} not found in database.");
            }

            // 创建 Agent
            return await CreateAgentAsync(config, scenario, ct);
        }

        /// <summary>
        /// 根据场景创建最佳 LLM Agent（自动选择优先级最高的）
        /// </summary>
        public async Task<MafAiAgent> CreateBestAgentForScenarioAsync(
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "[Factory] Finding best agent for scenario: {Scenario}",
                scenario);

            // 从数据库加载所有启用的配置
            var allConfigs = await _configRepository.GetAllEnabledAsync(ct);

            // 筛选支持该场景的配置
            var candidates = allConfigs
                .Where(c => c.SupportedScenarios.Contains(scenario))
                .OrderBy(c => c.Priority) // 优先级数字越小越高
                .ToList();

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No enabled provider found that supports scenario: {scenario}");
            }

            // 选择优先级最高的
            var bestConfig = candidates[0];
            _logger.LogInformation(
                "[Factory] Selected best provider: {ProviderName} (Priority: {Priority}) for scenario {Scenario}",
                bestConfig.ProviderName,
                bestConfig.Priority,
                scenario);

            return await CreateAgentAsync(bestConfig, scenario, ct);
        }

        /// <summary>
        /// 批量创建支持指定场景的所有 LLM Agent
        /// </summary>
        public async Task<List<MafAiAgent>> CreateAllAgentsForScenarioAsync(
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "[Factory] Creating all agents for scenario: {Scenario}",
                scenario);

            // 从数据库加载所有启用的配置
            var allConfigs = await _configRepository.GetAllEnabledAsync(ct);

            // 筛选支持该场景的配置
            var candidates = allConfigs
                .Where(c => c.SupportedScenarios.Contains(scenario))
                .OrderBy(c => c.Priority)
                .ToList();

            if (candidates.Count == 0)
            {
                _logger.LogWarning(
                    "[Factory] No enabled provider found for scenario: {Scenario}",
                    scenario);
                return new List<MafAiAgent>();
            }

            // 批量创建
            var agents = new List<MafAiAgent>();
            foreach (var config in candidates)
            {
                try
                {
                    var agent = await CreateAgentAsync(config, scenario, ct);
                    agents.Add(agent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[Factory] Failed to create agent for provider {ProviderName}",
                        config.ProviderName);
                }
            }

            _logger.LogInformation(
                "[Factory] Created {Count} agents for scenario {Scenario}",
                agents.Count,
                scenario);

            return agents;
        }

        /// <summary>
        /// 创建带有 Fallback 链的 LLM Agent
        /// </summary>
        public async Task<MafAiAgent> CreateAgentWithFallbackAsync(
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "[Factory] Creating agent with fallback for scenario: {Scenario}",
                scenario);

            // 获取所有支持该场景的 Agent（按优先级排序）
            var allAgents = await CreateAllAgentsForScenarioAsync(scenario, ct);

            if (allAgents.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No agent available for scenario: {scenario}");
            }

            // 如果只有一个 Agent，直接返回
            if (allAgents.Count == 1)
            {
                _logger.LogInformation(
                    "[Factory] Only one agent available, returning without fallback wrapper");
                return allAgents[0];
            }

            // 创建带 Fallback 能力的包装器
            var primaryAgent = allAgents[0];
            var fallbackAgents = allAgents.Skip(1).ToList();

            _logger.LogInformation(
                "[Factory] Creating FallbackLlmAgent with primary: {PrimaryAgentId} and {FallbackCount} fallbacks",
                primaryAgent.AgentId,
                fallbackAgents.Count);

            return new FallbackLlmAgent(primaryAgent, fallbackAgents, _logger);
        }

        /// <summary>
        /// 检查指定的提供商是否支持该场景
        /// </summary>
        public async Task<bool> IsScenarioSupportedAsync(
            string providerName,
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            try
            {
                var config = await _configRepository.GetByNameAsync(providerName, ct);
                return config?.SupportedScenarios.Contains(scenario) == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Factory] Error checking scenario support for provider {ProviderName}",
                    providerName);
                return false;
            }
        }

        #endregion

        #region 提供商特定创建方法

        /// <summary>
        /// 创建智谱AI Agent
        /// </summary>
        /// <remarks>
        /// ZhipuAIAgent 需要 HttpClient，通过 IHttpClientFactory 提供。
        /// 如果未提供 IHttpClientFactory，则抛出异常。
        /// </remarks>
        private async Task<MafAiAgent> CreateZhipuAIAgentAsync(
            LlmProviderConfig config,
            CancellationToken ct)
        {
            await Task.CompletedTask;

            if (_httpClientFactory == null)
            {
                throw new InvalidOperationException(
                    "ZhipuAIAgent requires IHttpClientFactory. " +
                    "Please register IHttpClientFactory in DI container: services.AddHttpClient();");
            }

            var logger = _loggerFactory.CreateLogger<ZhipuAIAgent>();
            var httpClient = _httpClientFactory.CreateClient(nameof(ZhipuAIAgent));

            // 配置 HttpClient
            httpClient.BaseAddress = new Uri(config.ApiBaseUrl ?? "https://open.bigmodel.cn/api/paas/v4/");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetApiKey(config)}");
            httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

            return new ZhipuAIAgent(config, logger, httpClient);
        }

        /// <summary>
        /// 创建通义千问 Agent
        /// </summary>
        private async Task<MafAiAgent> CreateTongyiAgentAsync(
            LlmProviderConfig config,
            CancellationToken ct)
        {
            await Task.CompletedTask;

            var logger = _loggerFactory.CreateLogger<TongyiLlmAgent>();
            HttpClient? httpClient = null;

            if (_httpClientFactory != null)
            {
                httpClient = _httpClientFactory.CreateClient(nameof(TongyiLlmAgent));
                httpClient.BaseAddress = new Uri(config.ApiBaseUrl ?? "https://dashscope.aliyuncs.com/compatible-mode/v1");
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetApiKey(config)}");
            }

            return new TongyiLlmAgent(config, logger, httpClient);
        }

        /// <summary>
        /// 创建 Qwen Agent
        /// </summary>
        private async Task<MafAiAgent> CreateQwenAgentAsync(
            LlmProviderConfig config,
            CancellationToken ct)
        {
            // Qwen 和通义千问使用相同的实现
            return await CreateTongyiAgentAsync(config, ct);
        }

        /// <summary>
        /// 创建文心一言 Agent
        /// </summary>
        private async Task<MafAiAgent> CreateWenxinAgentAsync(
            LlmProviderConfig config,
            CancellationToken ct)
        {
            await Task.CompletedTask;

            var logger = _loggerFactory.CreateLogger<WenxinLlmAgent>();
            HttpClient? httpClient = null;

            if (_httpClientFactory != null)
            {
                httpClient = _httpClientFactory.CreateClient(nameof(WenxinLlmAgent));
                httpClient.BaseAddress = new Uri(config.ApiBaseUrl ?? "https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat");
            }

            return new WenxinLlmAgent(config, logger, httpClient);
        }

        /// <summary>
        /// 创建讯飞星火 Agent
        /// </summary>
        private async Task<MafAiAgent> CreateXunfeiAgentAsync(
            LlmProviderConfig config,
            CancellationToken ct)
        {
            await Task.CompletedTask;

            var logger = _loggerFactory.CreateLogger<XunfeiLlmAgent>();
            HttpClient? httpClient = null;

            if (_httpClientFactory != null)
            {
                httpClient = _httpClientFactory.CreateClient(nameof(XunfeiLlmAgent));
            }

            return new XunfeiLlmAgent(config, logger, httpClient);
        }

        /// <summary>
        /// 创建百川 Agent
        /// </summary>
        private async Task<MafAiAgent> CreateBaichuanAgentAsync(
            LlmProviderConfig config,
            CancellationToken ct)
        {
            await Task.CompletedTask;

            var logger = _loggerFactory.CreateLogger<BaichuanLlmAgent>();
            HttpClient? httpClient = null;

            if (_httpClientFactory != null)
            {
                httpClient = _httpClientFactory.CreateClient(nameof(BaichuanLlmAgent));
                httpClient.BaseAddress = new Uri(config.ApiBaseUrl ?? "https://api.baichuan-ai.com/v1");
            }

            return new BaichuanLlmAgent(config, logger, httpClient);
        }

        /// <summary>
        /// 创建 MiniMax Agent
        /// </summary>
        private async Task<MafAiAgent> CreateMiniMaxAgentAsync(
            LlmProviderConfig config,
            CancellationToken ct)
        {
            await Task.CompletedTask;

            var logger = _loggerFactory.CreateLogger<MiniMaxLlmAgent>();
            HttpClient? httpClient = null;

            if (_httpClientFactory != null)
            {
                httpClient = _httpClientFactory.CreateClient(nameof(MiniMaxLlmAgent));
                httpClient.BaseAddress = new Uri(config.ApiBaseUrl ?? "https://api.minimax.chat/v1");
            }

            return new MiniMaxLlmAgent(config, logger, httpClient);
        }

        /// <summary>
        /// 从配置获取 API Key
        /// </summary>
        private static string GetApiKey(LlmProviderConfig config)
        {
            return config.ApiKey ?? string.Empty;
        }

        #endregion
    }
}
