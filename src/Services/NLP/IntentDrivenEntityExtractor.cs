using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 意图驱动的实体提取器
    /// 根据 Intent 识别结果选择对应的 EntityPatternProvider
    /// 支持关键字匹配 + LLM 增强的混合模式
    /// </summary>
    public class IntentDrivenEntityExtractor : IEntityExtractor
    {
        private readonly IIntentRecognizer _intentRecognizer;
        private readonly IIntentProviderMapping _mapping;
        private readonly ILlmService _llmService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAsyncPolicy _circuitBreakerPolicy;
        private readonly ILogger<IntentDrivenEntityExtractor> _logger;

        public IntentDrivenEntityExtractor(
            IIntentRecognizer intentRecognizer,
            IIntentProviderMapping mapping,
            ILlmService llmService,
            IServiceProvider serviceProvider,
            ILogger<IntentDrivenEntityExtractor> logger)
        {
            _intentRecognizer = intentRecognizer ?? throw new ArgumentNullException(nameof(intentRecognizer));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化熔断器
            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromMinutes(5),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogWarning(ex,
                            "LLM circuit breaker opened for {Duration}s due to: {Message}",
                            breakDelay.TotalSeconds, ex.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("LLM circuit breaker reset to closed state");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("LLM circuit breaker in half-open state");
                    });
        }

        /// <inheritdoc />
        public async Task<EntityExtractionResult> ExtractAsync(
            string userInput,
            CancellationToken ct = default)
        {
            try
            {
                // Stage 1: 识别意图
                var intentResult = await _intentRecognizer.RecognizeAsync(userInput, ct);
                _logger.LogDebug("Recognized intent: {Intent} for input: {Input}",
                    intentResult.PrimaryIntent, userInput);

                // Stage 2: 获取对应的 Provider
                var provider = ResolveProvider(intentResult.PrimaryIntent);
                if (provider == null)
                {
                    _logger.LogWarning("No provider found for intent: {Intent}", intentResult.PrimaryIntent);
                    return new EntityExtractionResult();
                }

                // Stage 3: 关键字匹配
                var keywordResult = await ExtractByKeywordsAsync(userInput, provider, ct);
                _logger.LogDebug("Keyword extraction found {Count} entities",
                    keywordResult.Entities.Count);

                // Stage 4: 判断是否启用 LLM
                if (!ShouldEnableLlm(userInput, keywordResult, provider))
                {
                    return keywordResult;
                }

                // Stage 5: LLM 提取（带熔断器）- TODO: Task 5
                _logger.LogDebug("LLM extraction not yet implemented, returning keyword result");
                return keywordResult;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error in entity extraction");
                throw;
            }
        }

        private IEntityPatternProvider? ResolveProvider(string intent)
        {
            var providerType = _mapping.GetProviderType(intent);
            if (providerType == null)
            {
                return null;
            }

            var provider = _serviceProvider.GetService(providerType) as IEntityPatternProvider;
            if (provider == null)
            {
                _logger.LogError("Provider type registered but not in DI: {TypeName}", providerType.Name);
            }

            return provider;
        }

        private Task<EntityExtractionResult> ExtractByKeywordsAsync(
            string userInput,
            IEntityPatternProvider provider,
            CancellationToken ct)
        {
            var result = new EntityExtractionResult();
            var supportedTypes = provider.GetSupportedEntityTypes();

            foreach (var entityType in supportedTypes)
            {
                var patterns = provider.GetPatterns(entityType);
                if (patterns != null)
                {
                    foreach (var pattern in patterns)
                    {
                        if (!string.IsNullOrEmpty(pattern))
                        {
                            var index = userInput.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0)
                            {
                                var entity = new Entity
                                {
                                    EntityType = entityType,
                                    EntityValue = pattern,
                                    StartPosition = index,
                                    EndPosition = index + pattern.Length,
                                    Confidence = 0.9
                                };
                                result.ExtractedEntities.Add(entity);

                                if (!result.Entities.ContainsKey(entityType))
                                {
                                    result.Entities[entityType] = pattern;
                                }
                            }
                        }
                    }
                }
            }

            return Task.FromResult(result);
        }

        private bool ShouldEnableLlm(string userInput, EntityExtractionResult keywordResult, IEntityPatternProvider provider)
        {
            // A. 长度阈值：> 20 字
            bool isLongInput = userInput.Length > 20;

            // B. 关键字稀疏度：< 40% 覆盖率
            var supportedTypes = provider.GetSupportedEntityTypes().ToList();
            double coverageRate = supportedTypes.Count > 0
                ? (double)keywordResult.Entities.Count / supportedTypes.Count
                : 0;
            bool isSparse = coverageRate < 0.4;

            // C. 包含模糊词汇
            bool hasVagueWords = DetectVagueWords(userInput);

            return isLongInput || isSparse || hasVagueWords;
        }

        private bool DetectVagueWords(string input)
        {
            var vagueWords = new[] {
                "那边", "所有", "全部", "除了", "以外",
                "那个", "这个", "它们", "大家", "各个"
            };

            return vagueWords.Any(word => input.Contains(word));
        }
    }
}
