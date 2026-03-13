using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
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

                // Stage 5: LLM 提取（带熔断器）
                EntityExtractionResult llmResult;
                try
                {
                    llmResult = await _circuitBreakerPolicy.ExecuteAsync(async () =>
                    {
                        return await ExtractByLlmAsync(userInput, provider, ct);
                    });
                }
                catch (BrokenCircuitException)
                {
                    _logger.LogWarning("LLM circuit breaker is open, using keyword-only result");
                    return keywordResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LLM extraction failed, using keyword-only result");
                    return keywordResult;
                }

                // Stage 6: 合并结果
                var mergedResult = MergeResults(keywordResult, llmResult);
                _logger.LogDebug("Final merged result has {Count} entities",
                    mergedResult.Entities.Count);
                return mergedResult;
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

        private async Task<EntityExtractionResult> ExtractByLlmAsync(
            string userInput,
            IEntityPatternProvider provider,
            CancellationToken ct)
        {
            var supportedTypes = provider.GetSupportedEntityTypes();
            var fewShotExamples = provider.GetFewShotExamples();

            var systemPrompt = $@"你是实体提取助手。请从用户输入中提取以下类型的实体：
{string.Join(", ", supportedTypes)}

要求：
1. 只提取明确存在的实体类型，不要编造
2. 返回JSON格式，键为实体类型，值为提取的值
3. 如果某个实体类型不存在于输入中，不要包含该字段
4. 数值类型（如温度、亮度）只提取数字部分

示例：
{fewShotExamples}

返回格式：{{""实体类型"": ""实体值""}}";

            try
            {
                var response = await _llmService.CompleteAsync(systemPrompt, userInput, ct);
                _logger.LogDebug("LLM response: {Response}", response);

                return ParseLlmResponse(response, supportedTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse LLM response");
                throw;
            }
        }

        private EntityExtractionResult ParseLlmResponse(
            string response,
            IEnumerable<string> supportedTypes)
        {
            var result = new EntityExtractionResult();

            try
            {
                // 尝试提取 JSON（处理可能的 markdown 代码块）
                var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                    response,
                    @"```json\s*(\{.*?\})\s*```",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                var jsonString = jsonMatch.Success
                    ? jsonMatch.Groups[1].Value
                    : response.Trim();

                // 去除可能的 markdown 标记
                jsonString = System.Text.RegularExpressions.Regex.Replace(
                    jsonString,
                    @"```.*?```",
                    "",
                    System.Text.RegularExpressions.RegexOptions.Singleline).Trim();

                // 解析 JSON
                using var document = System.Text.Json.JsonDocument.Parse(jsonString);
                var root = document.RootElement;

                foreach (var property in root.EnumerateObject())
                {
                    var entityType = property.Name.Trim();
                    var entityValue = property.Value.GetString();

                    if (!string.IsNullOrEmpty(entityValue) &&
                        supportedTypes.Contains(entityType, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Entities[entityType] = entityValue!;
                        result.ExtractedEntities.Add(new Entity
                        {
                            EntityType = entityType,
                            EntityValue = entityValue!,
                            StartPosition = -1, // LLM 无法提供位置信息
                            EndPosition = -1,
                            Confidence = 0.95 // LLM 默认置信度
                        });
                    }
                }

                _logger.LogDebug("Parsed {Count} entities from LLM response",
                    result.Entities.Count);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM response as JSON: {Response}", response);
                throw;
            }

            return result;
        }

        private EntityExtractionResult MergeResults(
            EntityExtractionResult keywordResult,
            EntityExtractionResult llmResult)
        {
            var merged = new EntityExtractionResult();

            // 获取所有实体类型的并集
            var allTypes = keywordResult.Entities.Keys
                .Concat(llmResult.Entities.Keys)
                .Distinct();

            foreach (var entityType in allTypes)
            {
                bool hasKeyword = keywordResult.Entities.ContainsKey(entityType);
                bool hasLlm = llmResult.Entities.ContainsKey(entityType);

                if (hasKeyword && hasLlm)
                {
                    // 两者都有：选择置信度高的
                    var keywordEntity = keywordResult.ExtractedEntities
                        .First(e => e.EntityType == entityType);
                    var llmEntity = llmResult.ExtractedEntities
                        .First(e => e.EntityType == entityType);

                    var winner = llmEntity.Confidence > keywordEntity.Confidence
                        ? llmEntity
                        : keywordEntity;

                    merged.Entities[entityType] = winner.EntityValue;
                    merged.ExtractedEntities.Add(winner);
                }
                else if (hasKeyword)
                {
                    // 仅关键字有：使用关键字结果
                    var keywordEntity = keywordResult.ExtractedEntities
                        .First(e => e.EntityType == entityType);
                    merged.Entities[entityType] = keywordEntity.EntityValue;
                    merged.ExtractedEntities.Add(keywordEntity);
                }
                else if (hasLlm)
                {
                    // 仅 LLM 有：使用 LLM 结果（补充）
                    var llmEntity = llmResult.ExtractedEntities
                        .First(e => e.EntityType == entityType);
                    merged.Entities[entityType] = llmEntity.EntityValue;
                    merged.ExtractedEntities.Add(llmEntity);
                }
            }

            return merged;
        }
    }
}
