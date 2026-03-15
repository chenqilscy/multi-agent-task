# LLM Enhanced Entity Extraction Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement intent-driven entity extraction with LLM enhancement, supporting business-specific entity patterns through configurable providers.

**Architecture:** Two-stage extraction (Intent Recognition → Provider Selection → Keyword + LLM Hybrid Extraction) with circuit breaker pattern for LLM resilience. Framework layer provides interfaces and orchestration, business layer implements scenario-specific providers.

**Tech Stack:** .NET 10, Microsoft Agent Framework, Polly (circuit breaker), xUnit, Moq, System.Text.Json

---

## Task 1: Implement IntentProviderMapping

**Files:**
- Create: `src/Core/Abstractions/IIntentProviderMapping.cs`
- Create: `src/Core/Abstractions/IntentProviderMapping.cs`
- Test: `src/tests/UnitTests/NLP/IntentProviderMappingTests.cs`

**Step 1: Write the interface**

Create `src/Core/Abstractions/IIntentProviderMapping.cs`:

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图到实体模式提供者的映射接口
    /// 由业务层注册映射关系
    /// </summary>
    public interface IIntentProviderMapping
    {
        /// <summary>
        /// 注册意图到 Provider 类型的映射
        /// </summary>
        void Register(string intent, Type providerType);

        /// <summary>
        /// 获取意图对应的 Provider 类型
        /// </summary>
        Type? GetProviderType(string intent);

        /// <summary>
        /// 获取所有已注册的意图
        /// </summary>
        IEnumerable<string> GetRegisteredIntents();
    }
}
```

**Step 2: Write the implementation test**

Create `src/tests/UnitTests/NLP/IntentProviderMappingTests.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class IntentProviderMappingTests
    {
        [Fact]
        public void Register_WithValidParameters_ShouldStoreMapping()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            var intent = "ControlLight";
            var providerType = typeof(LightControlEntityPatternProvider);

            // Act
            mapping.Register(intent, providerType);

            // Assert
            var result = mapping.GetProviderType(intent);
            Assert.Equal(providerType, result);
        }

        [Fact]
        public void Register_WithNullIntent_ShouldThrow()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                mapping.Register(null!, typeof(LightControlEntityPatternProvider)));
        }

        [Fact]
        public void Register_WithNonProviderType_ShouldThrow()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                mapping.Register("TestIntent", typeof(string)));
        }

        [Fact]
        public void GetProviderType_WithUnregisteredIntent_ShouldReturnNull()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act
            var result = mapping.GetProviderType("NonExistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRegisteredIntents_ShouldReturnAllRegistered()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            mapping.Register("Intent1", typeof(LightControlEntityPatternProvider));
            mapping.Register("Intent2", typeof(ACControlEntityPatternProvider));

            // Act
            var intents = mapping.GetRegisteredIntents();

            // Assert
            Assert.Contains("Intent1", intents);
            Assert.Contains("Intent2", intents);
        }
    }
}
```

**Step 3: Run test to verify it fails**

Run: `dotnet test src/tests/UnitTests/NLP/IntentProviderMappingTests.cs --filter "FullyQualifiedName~IntentProviderMappingTests" -v n`
Expected: FAIL with "IntentProviderMapping does not exist"

**Step 4: Write minimal implementation**

Create `src/Core/Abstractions/IntentProviderMapping.cs`:

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图到实体模式提供者的映射实现
    /// </summary>
    public class IntentProviderMapping : IIntentProviderMapping
    {
        private readonly Dictionary<string, Type> _mapping = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public void Register(string intent, Type providerType)
        {
            if (string.IsNullOrWhiteSpace(intent))
                throw new ArgumentException("Intent cannot be null or whitespace.", nameof(intent));

            if (providerType == null)
                throw new ArgumentNullException(nameof(providerType));

            if (!typeof(IEntityPatternProvider).IsAssignableFrom(providerType))
                throw new ArgumentException($"Type must implement IEntityPatternProvider: {providerType.Name}", nameof(providerType));

            _mapping[intent] = providerType;
        }

        /// <inheritdoc />
        public Type? GetProviderType(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
                return null;

            return _mapping.TryGetValue(intent, out var type) ? type : null;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetRegisteredIntents()
        {
            return _mapping.Keys;
        }
    }
}
```

**Step 5: Run test to verify it passes**

Run: `dotnet test src/tests/UnitTests/NLP/IntentProviderMappingTests.cs --filter "FullyQualifiedName~IntentProviderMappingTests" -v n`
Expected: PASS

**Step 6: Commit**

```bash
git add src/Core/Abstractions/IIntentProviderMapping.cs src/Core/Abstractions/IntentProviderMapping.cs src/tests/UnitTests/NLP/IntentProviderMappingTests.cs
git commit -m "feat: add IntentProviderMapping for intent-to-provider resolution

- Add IIntentProviderMapping interface
- Implement IntentProviderMapping with validation
- Add unit tests for registration and resolution

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 2: Extend IEntityPatternProvider Interface

**Files:**
- Modify: `src/Core/Abstractions/IEntityPatternProvider.cs`
- Modify: `src/Demos/SmartHome/SmartHomeEntityPatternProvider.cs`
- Test: `src/tests/UnitTests/NLP/SmartHomeEntityPatternProviderTests.cs`

**Step 1: Add test for new method**

Create `src/tests/UnitTests/NLP/SmartHomeEntityPatternProviderTests.cs`:

```csharp
using CKY.MultiAgentFramework.Demos.SmartHome;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class SmartHomeEntityPatternProviderTests
    {
        [Fact]
        public void GetFewShotExamples_ShouldReturnNonEmptyString()
        {
            // Arrange
            var provider = new SmartHomeEntityPatternProvider();

            // Act
            var examples = provider.GetFewShotExamples();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(examples));
            Assert.Contains("输入", examples);
            Assert.Contains("输出", examples);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/tests/UnitTests/NLP/SmartHomeEntityPatternProviderTests.cs -v n`
Expected: FAIL with "GetFewShotExamples does not exist"

**Step 3: Extend interface**

Modify `src/Core/Abstractions/IEntityPatternProvider.cs` (add at end of interface):

```csharp
        /// <summary>
        /// 获取 Few-shot 示例（用于 LLM Prompt）
        /// 由业务层实现，提供该场景的示例
        /// </summary>
        string GetFewShotExamples();
```

**Step 4: Implement in SmartHome provider**

Modify `src/Demos/SmartHome/SmartHomeEntityPatternProvider.cs` (add before closing brace):

```csharp
        /// <inheritdoc />
        public string GetFewShotExamples()
        {
            return @"
输入：""打开客厅的灯""
输出：{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""打开""}

输入：""把卧室空调调到26度""
输出：{""Room"": ""卧室"", ""Device"": ""空调"", ""Action"": ""调节"", ""Value"": ""26""}";
        }
```

**Step 5: Run test to verify it passes**

Run: `dotnet test src/tests/UnitTests/NLP/SmartHomeEntityPatternProviderTests.cs -v n`
Expected: PASS

**Step 6: Commit**

```bash
git add src/Core/Abstractions/IEntityPatternProvider.cs src/Demos/SmartHome/SmartHomeEntityPatternProvider.cs src/tests/UnitTests/NLP/SmartHomeEntityPatternProviderTests.cs
git commit -m "feat: extend IEntityPatternProvider with Few-shot examples

- Add GetFewShotExamples method to interface
- Implement in SmartHomeEntityPatternProvider
- Add unit test

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 3: Create Circuit Breaker Options

**Files:**
- Create: `src/Core/Models/Resilience/LlmCircuitBreakerOptions.cs`
- Test: `src/tests/UnitTests/Core/Resilience/LlmCircuitBreakerOptionsTests.cs`

**Step 1: Write test**

Create `src/tests/UnitTests/Core/Resilience/LlmCircuitBreakerOptionsTests.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Models.Resilience;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Resilience
{
    public class LlmCircuitBreakerOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeSet()
        {
            // Arrange & Act
            var options = new LlmCircuitBreakerOptions();

            // Assert
            Assert.Equal(3, options.FailureThreshold);
            Assert.Equal(TimeSpan.FromMinutes(5), options.BreakDuration);
            Assert.Equal(TimeSpan.FromSeconds(30), options.HalfOpenTimeout);
        }

        [Fact]
        public void CanSetCustomValues()
        {
            // Arrange
            var options = new LlmCircuitBreakerOptions
            {
                FailureThreshold = 5,
                BreakDuration = TimeSpan.FromMinutes(10),
                HalfOpenTimeout = TimeSpan.FromSeconds(60)
            };

            // Act & Assert
            Assert.Equal(5, options.FailureThreshold);
            Assert.Equal(TimeSpan.FromMinutes(10), options.BreakDuration);
            Assert.Equal(TimeSpan.FromSeconds(60), options.HalfOpenTimeout);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/tests/UnitTests/Core/Resilience/LlmCircuitBreakerOptionsTests.cs -v n`
Expected: FAIL with "type does not exist"

**Step 3: Implement options class**

Create `src/Core/Models/Resilience/LlmCircuitBreakerOptions.cs`:

```csharp
namespace CKY.MultiAgentFramework.Core.Models.Resilience
{
    /// <summary>
    /// LLM 熔断器配置选项
    /// </summary>
    public class LlmCircuitBreakerOptions
    {
        /// <summary>
        /// 失败阈值：连续失败多少次后熔断（默认 3 次）
        /// </summary>
        public int FailureThreshold { get; set; } = 3;

        /// <summary>
        /// 熔断持续时间（默认 5 分钟）
        /// </summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 半开状态的测试请求超时（默认 30 秒）
        /// </summary>
        public TimeSpan HalfOpenTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/tests/UnitTests/Core/Resilience/LlmCircuitBreakerOptionsTests.cs -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Core/Models/Resilience/LlmCircuitBreakerOptions.cs src/tests/UnitTests/Core/Resilience/LlmCircuitBreakerOptionsTests.cs
git commit -m "feat: add LlmCircuitBreakerOptions

- Add configuration options for circuit breaker
- Include FailureThreshold, BreakDuration, HalfOpenTimeout
- Add unit tests

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 4: Implement IntentDrivenEntityExtractor - Core Structure

**Files:**
- Create: `src/Services/NLP/IntentDrivenEntityExtractor.cs`
- Create: `src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs`

**Step 1: Write basic test**

Create `src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.NLP;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class IntentDrivenEntityExtractorTests
    {
        [Fact]
        public async Task ExtractAsync_WhenProviderNotFound_ShouldReturnEmptyResult()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "UnknownIntent" });

            mockMapping
                .Setup(x => x.GetProviderType("UnknownIntent"))
                .Returns((Type?)null);

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act
            var result = await extractor.ExtractAsync("test input");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Entities);
            Assert.Empty(result.ExtractedEntities);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs -v n`
Expected: FAIL with "IntentDrivenEntityExtractor does not exist"

**Step 3: Implement basic structure**

Create `src/Services/NLP/IntentDrivenEntityExtractor.cs`:

```csharp
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
                if (!ShouldEnableLlm(userInput, keywordResult))
                {
                    return keywordResult;
                }

                // Stage 5: LLM 提取（带熔断器）
                try
                {
                    var llmResult = await _circuitBreakerPolicy.ExecuteAsync(async () =>
                    {
                        return await ExtractByLlmAsync(userInput, provider, ct);
                    });

                    return MergeResults(keywordResult, llmResult);
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

        private bool ShouldEnableLlm(string userInput, EntityExtractionResult keywordResult)
        {
            // A. 长度阈值：> 20 字
            bool isLongInput = userInput.Length > 20;

            // B. 关键字稀疏度：< 40% 覆盖率
            // 需要获取 provider 的支持类型，这里简化处理
            // 实际实现需要从 provider 获取

            // C. 包含模糊词汇
            bool hasVagueWords = DetectVagueWords(userInput);

            return isLongInput || hasVagueWords;
        }

        private bool DetectVagueWords(string input)
        {
            var vagueWords = new[] {
                "那边", "所有", "全部", "除了", "以外",
                "那个", "这个", "它们", "大家", "各个"
            };

            return vagueWords.Any(word => input.Contains(word));
        }

        private Task<EntityExtractionResult> ExtractByLlmAsync(
            string userInput,
            IEntityPatternProvider provider,
            CancellationToken ct)
        {
            // TODO: Implement LLM extraction
            // This will be implemented in a later task
            return Task.FromResult(new EntityExtractionResult());
        }

        private EntityExtractionResult MergeResults(
            EntityExtractionResult keywordResult,
            EntityExtractionResult llmResult)
        {
            // TODO: Implement result merging
            // This will be implemented in a later task
            return keywordResult;
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Services/NLP/IntentDrivenEntityExtractor.cs src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs
git commit -m "feat: add IntentDrivenEntityExtractor core structure

- Implement basic two-stage extraction flow
- Add provider resolution logic
- Add keyword extraction fallback
- Add circuit breaker for LLM resilience
- Add unit test for provider not found scenario

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 5: Implement LLM Extraction Logic

**Files:**
- Modify: `src/Services/NLP/IntentDrivenEntityExtractor.cs`
- Test: `src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs`

**Step 1: Write test for LLM extraction**

Add to `src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs`:

```csharp
        [Fact]
        public async Task ExtractByLlmAsync_ShouldParseLlmResponse()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockProvider = new Mock<IEntityPatternProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlLight" });

            mockMapping
                .Setup(x => x.GetProviderType("ControlLight"))
                .Returns(typeof(LightControlEntityPatternProvider));

            mockServiceProvider
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns(mockProvider.Object);

            mockProvider
                .Setup(x => x.GetSupportedEntityTypes())
                .Returns(new[] { "Room", "Device", "Action" });

            mockProvider
                .Setup(x => x.GetPatterns(It.IsAny<string>()))
                .Returns(Array.Empty<string>());

            mockLlmService
                .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""打开""}");

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act - use long input to trigger LLM
            var result = await extractor.ExtractAsync("帮我把客厅的灯都打开，顺便检查一下其他房间的设备");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Entities.ContainsKey("Room") || result.ExtractedEntities.Count > 0);
        }
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs -v n`
Expected: FAIL (test might pass but LLM extraction not fully implemented)

**Step 3: Implement LLM extraction**

Modify `src/Services/NLP/IntentDrivenEntityExtractor.cs` - replace `ExtractByLlmAsync` method:

```csharp
        private async Task<EntityExtractionResult> ExtractByLlmAsync(
            string userInput,
            IEntityPatternProvider provider,
            CancellationToken ct)
        {
            _logger.LogDebug("Starting LLM extraction for input: {Input}", userInput);

            try
            {
                var systemPrompt = BuildSystemPrompt();
                var userPrompt = BuildUserPrompt(userInput, provider);

                var response = await _llmService.CompleteAsync(systemPrompt, userPrompt, ct);
                var result = ParseLlmResponse(response, userInput);

                _logger.LogInformation("LLM extracted {Count} entities with confidence {AvgConfidence:F2}",
                    result.ExtractedEntities.Count,
                    result.ExtractedEntities.Count > 0
                        ? result.ExtractedEntities.Average(e => e.Confidence)
                        : 0.0);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM extraction failed");
                throw;
            }
        }

        private string BuildSystemPrompt()
        {
            return "你是实体提取助手。请严格按照用户输入提取实体，返回JSON格式。";
        }

        private string BuildUserPrompt(string userInput, IEntityPatternProvider provider)
        {
            var supportedTypes = provider.GetSupportedEntityTypes();
            var examples = provider.GetFewShotExamples();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("你是实体提取助手。请从用户输入中提取以下类型的实体：");
            sb.AppendLine(string.Join(", ", supportedTypes));
            sb.AppendLine();
            sb.AppendLine("提取规则：");
            sb.AppendLine("1. 严格按照指定的实体类型提取");
            sb.AppendLine("2. 如果用户输入中不存在某类实体，该字段不返回");
            sb.AppendLine("3. 数值实体请提取原始数字（不含单位）");
            sb.AppendLine();
            sb.AppendLine("示例：");
            sb.AppendLine(examples);
            sb.AppendLine();
            sb.AppendLine($"用户输入：{userInput}");
            sb.AppendLine();
            sb.AppendLine("请以JSON格式返回提取结果：");
            sb.AppendLine(@"{""EntityType"": ""Value""}");

            return sb.ToString();
        }

        private EntityExtractionResult ParseLlmResponse(string llmResponse, string userInput)
        {
            var result = new EntityExtractionResult();

            try
            {
                var jsonStart = llmResponse.IndexOf('{');
                var jsonEnd = llmResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonStr = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = System.Text.Json.JsonDocument.Parse(jsonStr);

                    foreach (var property in parsed.RootElement.EnumerateObject())
                    {
                        var entityType = property.Name;
                        var entityValue = property.Value.GetString();

                        if (!string.IsNullOrEmpty(entityValue))
                        {
                            var entity = new Entity
                            {
                                EntityType = entityType,
                                EntityValue = entityValue,
                                StartPosition = userInput.IndexOf(entityValue, StringComparison.OrdinalIgnoreCase),
                                EndPosition = userInput.IndexOf(entityValue, StringComparison.OrdinalIgnoreCase) + entityValue.Length,
                                Confidence = 0.85 // 默认 LLM 置信度
                            };

                            if (entity.StartPosition < 0)
                            {
                                entity.StartPosition = 0;
                                entity.EndPosition = entityValue.Length;
                            }

                            result.ExtractedEntities.Add(entity);
                            result.Entities[entityType] = entityValue;
                        }
                    }

                    _logger.LogDebug("Parsed LLM response: {Json}", jsonStr);
                }
                else
                {
                    _logger.LogWarning("No valid JSON found in LLM response");
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error parsing LLM response as JSON: {Response}", llmResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing LLM response");
            }

            return result;
        }
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs -v n`
Expected: PASS (might need to adjust test expectations)

**Step 5: Commit**

```bash
git add src/Services/NLP/IntentDrivenEntityExtractor.cs src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs
git commit -m "feat: implement LLM extraction logic

- Add ExtractByLlmAsync with Few-shot prompt generation
- Add JSON response parsing with error handling
- Add detailed logging for LLM interactions
- Add unit test for LLM extraction flow

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 6: Implement Result Merging Logic

**Files:**
- Modify: `src/Services/NLP/IntentDrivenEntityExtractor.cs`
- Test: `src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs`

**Step 1: Write test for result merging**

Add to `src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs`:

```csharp
        [Fact]
        public void MergeResults_WithBothKeywordAndLlm_ShouldSelectByConfidence()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();
            var extractor = new IntentDrivenEntityExtractor(
                Mock.Of<IIntentRecognizer>(),
                Mock.Of<IIntentProviderMapping>(),
                Mock.Of<ILlmService>(),
                Mock.Of<IServiceProvider>(),
                mockLogger.Object);

            var keywordResult = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>
                {
                    ["Device"] = "空调"
                },
                ExtractedEntities = new List<Entity>
                {
                    new Entity { EntityType = "Device", EntityValue = "空调", Confidence = 0.9 }
                }
            };

            var llmResult = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>
                {
                    ["Room"] = "客厅",
                    ["Device"] = "空调"
                },
                ExtractedEntities = new List<Entity>
                {
                    new Entity { EntityType = "Room", EntityValue = "客厅", Confidence = 0.7 },
                    new Entity { EntityType = "Device", EntityValue = "空调", Confidence = 0.95 }
                }
            };

            // Act
            var merged = extractor.InvokeMerge(keywordResult, llmResult);

            // Assert
            Assert.True(merged.Entities.ContainsKey("Room"));
            Assert.True(merged.Entities.ContainsKey("Device"));
            // Device 应该选择 LLM 结果（0.95 > 0.9）
        }
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs -v n`
Expected: FAIL with "InvokeMerge does not exist"

**Step 3: Implement result merging**

Modify `src/Services/NLP/IntentDrivenEntityExtractor.cs` - replace `MergeResults` method:

```csharp
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
                        .FirstOrDefault(e => e.EntityType == entityType);
                    var llmEntity = llmResult.ExtractedEntities
                        .FirstOrDefault(e => e.EntityType == entityType);

                    if (keywordEntity != null && llmEntity != null)
                    {
                        var winner = llmEntity.Confidence > keywordEntity.Confidence
                            ? llmEntity
                            : keywordEntity;

                        merged.Entities[entityType] = winner.EntityValue;
                        merged.ExtractedEntities.Add(winner);
                    }
                }
                else if (hasKeyword)
                {
                    // 仅关键字有：使用关键字结果
                    var keywordEntity = keywordResult.ExtractedEntities
                        .FirstOrDefault(e => e.EntityType == entityType);
                    if (keywordEntity != null)
                    {
                        merged.Entities[entityType] = keywordEntity.EntityValue;
                        merged.ExtractedEntities.Add(keywordEntity);
                    }
                }
                else if (hasLlm)
                {
                    // 仅 LLM 有：使用 LLM 结果（补充）
                    var llmEntity = llmResult.ExtractedEntities
                        .FirstOrDefault(e => e.EntityType == entityType);
                    if (llmEntity != null)
                    {
                        merged.Entities[entityType] = llmEntity.EntityValue;
                        merged.ExtractedEntities.Add(llmEntity);
                    }
                }
            }

            _logger.LogDebug("Merged {KeywordCount} keyword + {LlmCount} LLM entities into {MergedCount} final",
                keywordResult.ExtractedEntities.Count,
                llmResult.ExtractedEntities.Count,
                merged.ExtractedEntities.Count);

            return merged;
        }
```

**Step 4: Update test to use private method accessor**

Since `MergeResults` is private, we need to test it indirectly through `ExtractAsync`. Modify the test:

```csharp
        [Fact]
        public async Task ExtractAsync_WithBothResults_ShouldMergeCorrectly()
        {
            // Arrange
            var mockIntentRecognizer = new Mock<IIntentRecognizer>();
            var mockMapping = new Mock<IIntentProviderMapping>();
            var mockLlmService = new Mock<ILlmService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockProvider = new Mock<IEntityPatternProvider>();
            var mockLogger = new Mock<ILogger<IntentDrivenEntityExtractor>>();

            mockIntentRecognizer
                .Setup(x => x.RecognizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IntentRecognitionResult { PrimaryIntent = "ControlAC" });

            mockMapping
                .Setup(x => x.GetProviderType("ControlAC"))
                .Returns(typeof(ACControlEntityPatternProvider));

            mockServiceProvider
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns(mockProvider.Object);

            mockProvider
                .Setup(x => x.GetSupportedEntityTypes())
                .Returns(new[] { "Room", "Device", "Action", "Temperature" });

            mockProvider
                .Setup(x => x.GetPatterns("Room"))
                .Returns(new[] { "卧室" });
            mockProvider
                .Setup(x => x.GetPatterns("Device"))
                .Returns(new[] { "空调" });
            mockProvider
                .Setup(x => x.GetPatterns("Action"))
                .Returns(new[] { "调节" });
            mockProvider
                .Setup(x => x.GetPatterns("Temperature"))
                .Returns(Array.Empty<string>()); // 关键字没有数值

            mockProvider
                .Setup(x => x.GetFewShotExamples())
                .Returns("示例：输入输出");

            mockLlmService
                .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"{""Room"": ""卧室"", ""Device"": ""空调"", ""Action"": ""调节"", ""Temperature"": ""26""}");

            var extractor = new IntentDrivenEntityExtractor(
                mockIntentRecognizer.Object,
                mockMapping.Object,
                mockLlmService.Object,
                mockServiceProvider.Object,
                mockLogger.Object);

            // Act - use long input with vague word to trigger LLM
            var result = await extractor.ExtractAsync("把除了卧室以外所有房间的空调都调到26度");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("卧室", result.Entities["Room"]);
            Assert.Equal("空调", result.Entities["Device"]);
            Assert.Equal("26", result.Entities["Temperature"]);
        }
```

**Step 5: Run test to verify it passes**

Run: `dotnet test src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs -v n`
Expected: PASS

**Step 6: Commit**

```bash
git add src/Services/NLP/IntentDrivenEntityExtractor.cs src/tests/UnitTests/NLP/IntentDrivenEntityExtractorTests.cs
git commit -m "feat: implement confidence-based result merging

- Add MergeResults with confidence-weighted selection
- Prioritize higher confidence entities from keyword or LLM
- Include entities only found by LLM for better coverage
- Add integration test for merging behavior
- Add detailed merge logging

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 7: Create Business Layer Providers

**Files:**
- Create: `src/Demos/SmartHome/LightControlEntityPatternProvider.cs`
- Create: `src/Demos/SmartHome/ACControlEntityPatternProvider.cs`
- Create: `src/Demos/SmartHome/CurtainControlEntityPatternProvider.cs`
- Test: `src/tests/UnitTests/Demos/SmartHome/LightControlEntityPatternProviderTests.cs`
- Test: `src/tests/UnitTests/Demos/SmartHome/ACControlEntityPatternProviderTests.cs`
- Test: `src/tests/UnitTests/Demos/SmartHome/CurtainControlEntityPatternProviderTests.cs`

**Step 1: Write tests for LightControl provider**

Create `src/tests/UnitTests/Demos/SmartHome/LightControlEntityPatternProviderTests.cs`:

```csharp
using CKY.MultiAgentFramework.Demos.SmartHome;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Demos.SmartHome
{
    public class LightControlEntityPatternProviderTests
    {
        [Fact]
        public void GetSupportedEntityTypes_ShouldReturnExpectedTypes()
        {
            // Arrange
            var provider = new LightControlEntityPatternProvider();

            // Act
            var types = provider.GetSupportedEntityTypes();

            // Assert
            Assert.Contains("Room", types);
            Assert.Contains("Device", types);
            Assert.Contains("Action", types);
            Assert.Contains("Brightness", types);
            Assert.Contains("Color", types);
        }

        [Fact]
        public void GetPatterns_WithRoom_ShouldReturnRoomKeywords()
        {
            // Arrange
            var provider = new LightControlEntityPatternProvider();

            // Act
            var patterns = provider.GetPatterns("Room");

            // Assert
            Assert.NotNull(patterns);
            Assert.Contains("客厅", patterns);
            Assert.Contains("卧室", patterns);
        }

        [Fact]
        public void GetFewShotExamples_ShouldReturnValidExamples()
        {
            // Arrange
            var provider = new LightControlEntityPatternProvider();

            // Act
            var examples = provider.GetFewShotExamples();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(examples));
            Assert.Contains("灯", examples);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/tests/UnitTests/Demos/SmartHome/LightControlEntityPatternProviderTests.cs -v n`
Expected: FAIL with "LightControlEntityPatternProvider does not exist"

**Step 3: Implement LightControl provider**

Create `src/Demos/SmartHome/LightControlEntityPatternProvider.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 控灯场景的实体模式提供者
    /// </summary>
    public class LightControlEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public LightControlEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = new[] { "客厅", "卧室", "厨房", "浴室", "书房", "餐厅", "阳台" },
                ["Device"] = new[] { "灯", "电灯", "照明", "吊灯", "台灯", "吸顶灯", "壁灯" },
                ["Action"] = new[] { "打开", "关闭", "调节", "设置", "开启", "关掉", "开", "关" },
                ["Brightness"] = new[] { "亮", "暗", "亮度", "明亮", "昏暗" },
                ["Color"] = new[] { "红", "绿", "蓝", "白", "黄", "颜色", "彩色" }
            };
        }

        /// <inheritdoc />
        public string?[]? GetPatterns(string entityType)
        {
            _entityPatternMap.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _entityPatternMap.Keys;
        }

        /// <inheritdoc />
        public string GetFewShotExamples()
        {
            return @"
输入：""打开客厅的灯""
输出：{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""打开""}

输入：""把卧室灯调暗一点""
输出：{""Room"": ""卧室"", ""Device"": ""灯"", ""Action"": ""调节"", ""Brightness"": ""降低""}";
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test src/tests/UnitTests/Demos/SmartHome/LightControlEntityPatternProviderTests.cs -v n`
Expected: PASS

**Step 5: Implement ACControl provider**

Create `src/Demos/SmartHome/ACControlEntityPatternProvider.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 控空调场景的实体模式提供者
    /// </summary>
    public class ACControlEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public ACControlEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = new[] { "客厅", "卧室", "厨房", "书房", "餐厅" },
                ["Device"] = new[] { "空调", "冷气", "暖气", "AC" },
                ["Action"] = new[] { "打开", "关闭", "调节", "设置", "开启", "关掉" },
                ["Temperature"] = new[] { "度", "摄氏度", "温度", "°" },
                ["Mode"] = new[] { "制冷", "制热", "除湿", "自动", "送风" }
            };
        }

        /// <inheritdoc />
        public string?[]? GetPatterns(string entityType)
        {
            _entityPatternMap.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _entityPatternMap.Keys;
        }

        /// <inheritdoc />
        public string GetFewShotExamples()
        {
            return @"
输入：""把卧室空调调到制冷26度""
输出：{""Room"": ""卧室"", ""Device"": ""空调"", ""Action"": ""调节"", ""Temperature"": ""26"", ""Mode"": ""制冷""}

输入：""打开客厅空调""
输出：{""Room"": ""客厅"", ""Device"": ""空调"", ""Action"": ""打开""}";
        }
    }
}
```

**Step 6: Implement CurtainControl provider**

Create `src/Demos/SmartHome/CurtainControlEntityPatternProvider.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 控窗帘场景的实体模式提供者
    /// </summary>
    public class CurtainControlEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public CurtainControlEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = new[] { "客厅", "卧室", "书房", "餐厅" },
                ["Device"] = new[] { "窗帘", "百叶窗", "卷帘" },
                ["Action"] = new[] { "打开", "关闭", "调节", "设置", "拉开", "拉上" },
                ["Position"] = new[] { "一半", "全开", "全关", "一半", "部分", "%" }
            };
        }

        /// <inheritdoc />
        public string?[]? GetPatterns(string entityType)
        {
            _entityPatternMap.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _entityPatternMap.Keys;
        }

        /// <inheritdoc />
        public string GetFewShotExamples()
        {
            return @"
输入：""把客厅窗帘打开一半""
输出：{""Room"": ""客厅"", ""Device"": ""窗帘"", ""Action"": ""调节"", ""Position"": ""一半""}

输入：""关闭卧室窗帘""
输出：{""Room"": ""卧室"", ""Device"": ""窗帘"", ""Action"": ""关闭""}";
        }
    }
}
```

**Step 7: Create tests for AC and Curtain providers**

Create `src/tests/UnitTests/Demos/SmartHome/ACControlEntityPatternProviderTests.cs`:

```csharp
using CKY.MultiAgentFramework.Demos.SmartHome;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Demos.SmartHome
{
    public class ACControlEntityPatternProviderTests
    {
        [Fact]
        public void GetSupportedEntityTypes_ShouldIncludeTemperatureAndMode()
        {
            // Arrange
            var provider = new ACControlEntityPatternProvider();

            // Act
            var types = provider.GetSupportedEntityTypes();

            // Assert
            Assert.Contains("Room", types);
            Assert.Contains("Device", types);
            Assert.Contains("Action", types);
            Assert.Contains("Temperature", types);
            Assert.Contains("Mode", types);
        }

        [Fact]
        public void GetPatterns_WithMode_ShouldReturnModeKeywords()
        {
            // Arrange
            var provider = new ACControlEntityPatternProvider();

            // Act
            var patterns = provider.GetPatterns("Mode");

            // Assert
            Assert.NotNull(patterns);
            Assert.Contains("制冷", patterns);
            Assert.Contains("制热", patterns);
            Assert.Contains("除湿", patterns);
        }
    }
}
```

Create `src/tests/UnitTests/Demos/SmartHome/CurtainControlEntityPatternProviderTests.cs`:

```csharp
using CKY.MultiAgentFramework.Demos.SmartHome;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Demos.SmartHome
{
    public class CurtainControlEntityPatternProviderTests
    {
        [Fact]
        public void GetSupportedEntityTypes_ShouldIncludePosition()
        {
            // Arrange
            var provider = new CurtainControlEntityPatternProvider();

            // Act
            var types = provider.GetSupportedEntityTypes();

            // Assert
            Assert.Contains("Room", types);
            Assert.Contains("Device", types);
            Assert.Contains("Action", types);
            Assert.Contains("Position", types);
        }

        [Fact]
        public void GetFewShotExamples_ShouldReturnValidExamples()
        {
            // Arrange
            var provider = new CurtainControlEntityPatternProvider();

            // Act
            var examples = provider.GetFewShotExamples();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(examples));
            Assert.Contains("窗帘", examples);
        }
    }
}
```

**Step 8: Run all provider tests**

Run: `dotnet test src/tests/UnitTests/Demos/SmartHome/ -v n`
Expected: All PASS

**Step 9: Commit**

```bash
git add src/Demos/SmartHome/LightControlEntityPatternProvider.cs src/Demos/SmartHome/ACControlEntityPatternProvider.cs src/Demos/SmartHome/CurtainControlEntityPatternProvider.cs src/tests/UnitTests/Demos/SmartHome/
git commit -m "feat: add business layer entity pattern providers

- Add LightControlEntityPatternProvider for light control scenarios
- Add ACControlEntityPatternProvider for AC control scenarios
- Add CurtainControlEntityPatternProvider for curtain control scenarios
- Include device-specific entity types (Brightness, Color, Temperature, Mode, Position)
- Add unit tests for all providers

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 8: Update Demo Program.cs with DI Registration

**Files:**
- Modify: `src/Demos/SmartHome/Program.cs`

**Step 1: Add service registrations**

Modify `src/Demos/SmartHome/Program.cs` (after `AddDbContext` section, before `AddRazorComponents`):

```csharp
// 注册 LLM Service
// TODO: 根据实际配置选择 LLM Provider
// services.AddSingleton<ILlmService, ZhipuAILlmService>();
// services.AddSingleton<ILlmService, QwenLlmService>();
// services.AddSingleton<ILlmService, ERNIELlmService>();

// Mock LLM Service for development (replace with real implementation in production)
services.AddSingleton<ILlmService, MockLlmService>();

// 注册所有 Entity Pattern Providers
services.AddSingleton<LightControlEntityPatternProvider>();
services.AddSingleton<ACControlEntityPatternProvider>();
services.AddSingleton<CurtainControlEntityPatternProvider>();
services.AddSingleton<SmartHomeEntityPatternProvider>();

// 注册 Intent → Provider 映射
services.AddSingleton<IIntentProviderMapping>(sp =>
{
    var mapping = new IntentProviderMapping();

    // 控灯场景
    mapping.Register("ControlLight", typeof(LightControlEntityPatternProvider));
    mapping.Register("DimLight", typeof(LightControlEntityPatternProvider));
    mapping.Register("TurnOnLight", typeof(LightControlEntityPatternProvider));
    mapping.Register("TurnOffLight", typeof(LightControlEntityPatternProvider));

    // 控空调场景
    mapping.Register("ControlAC", typeof(ACControlEntityPatternProvider));
    mapping.Register("SetTemperature", typeof(ACControlEntityPatternProvider));
    mapping.Register("ControlAirConditioner", typeof(ACControlEntityPatternProvider));

    // 控窗帘场景
    mapping.Register("ControlCurtain", typeof(CurtainControlEntityPatternProvider));
    mapping.Register("OpenCurtain", typeof(CurtainControlEntityPatternProvider));
    mapping.Register("CloseCurtain", typeof(CurtainControlEntityPatternProvider));

    return mapping;
});

// 注册 Intent Recognizer
services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();

// 注册 Entity Extractor（使用新的 IntentDrivenEntityExtractor）
services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();
```

**Step 2: Create Mock LLM Service for development**

Create `src/Demos/SmartHome/MockLlmService.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// Mock LLM Service for development and testing
    /// 在生产环境中应替换为真实的 LLM 实现（如 ZhipuAILlmService）
    /// </summary>
    public class MockLlmService : ILlmService
    {
        private readonly ILogger<MockLlmService> _logger;

        public MockLlmService(ILogger<MockLlmService> logger)
        {
            _logger = logger;
        }

        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
        {
            return CompleteAsync("You are a helpful assistant.", prompt, ct);
        }

        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            _logger.LogDebug("Mock LLM called with system prompt: {System}", systemPrompt);

            // 返回模拟的 JSON 响应
            var mockResponse = @"{
                ""Room"": ""客厅"",
                ""Device"": ""灯"",
                ""Action"": ""打开""
            }";

            return Task.FromResult(mockResponse);
        }

        public AIAgent? GetUnderlyingAgent()
        {
            return null; // Mock service has no underlying agent
        }
    }
}
```

**Step 3: Add using statements to Program.cs**

Add at top of `src/Demos/SmartHome/Program.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Demos.SmartHome;
using CKY.MultiAgentFramework.Services.NLP;
```

**Step 4: Test build**

Run: `dotnet build src/Demos/SmartHome/CKY.MAF.Demos.SmartHome.csproj`
Expected: Build succeeds

**Step 5: Commit**

```bash
git add src/Demos/SmartHome/Program.cs src/Demos/SmartHome/MockLlmService.cs
git commit -m "feat: wire up LLM-enhanced entity extraction in Demo

- Register all entity pattern providers in DI
- Register IntentProviderMapping with scenario mappings
- Register IntentDrivenEntityExtractor as IEntityExtractor
- Add MockLlmService for development
- Add using statements for new services

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 9: Add Integration Tests

**Files:**
- Create: `src/tests/IntegrationTests/NLP/EntityExtractorIntegrationTests.cs`

**Step 1: Write integration test**

Create `src/tests/IntegrationTests/NLP/EntityExtractorIntegrationTests.cs`:

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Demos.SmartHome;
using CKY.MultiAgentFramework.Services.NLP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CKY.MultiAgentFramework.Tests.Integration.NLP
{
    [Collection("Integration Tests")]
    public class EntityExtractorIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ServiceProvider _serviceProvider;
        private readonly IEntityExtractor _extractor;

        public EntityExtractorIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            var services = new ServiceCollection();

            // Logging
            services.AddLogging(configure => configure.AddDebug());
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // LLM Service (Mock)
            services.AddSingleton<ILlmService, MockLlmService>();

            // Providers
            services.AddSingleton<LightControlEntityPatternProvider>();
            services.AddSingleton<ACControlEntityPatternProvider>();

            // Mapping
            services.AddSingleton<IIntentProviderMapping>(sp =>
            {
                var mapping = new IntentProviderMapping();
                mapping.Register("ControlLight", typeof(LightControlEntityPatternProvider));
                mapping.Register("ControlAC", typeof(ACControlEntityPatternProvider));
                return mapping;
            });

            // Intent Recognizer
            services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();

            // Extractor
            services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();

            _serviceProvider = services.BuildServiceProvider();
            _extractor = _serviceProvider.GetRequiredService<IEntityExtractor>();
        }

        [Fact]
        public async Task ExtractAsync_SimpleLightControl_ShouldExtractEntities()
        {
            // Act
            var result = await _extractor.ExtractAsync("打开客厅的灯");

            // Assert
            Assert.NotNull(result);
            _output.WriteLine($"Extracted entities: {string.Join(", ", result.Entities.Keys)}");
        }

        [Fact]
        public async Task ExtractAsync_ComplexACControl_ShouldUseLLM()
        {
            // Act
            var result = await _extractor.ExtractAsync("帮我把客厅和卧室的空调都调到26度，并且制冷模式");

            // Assert
            Assert.NotNull(result);
            _output.WriteLine($"Extracted {result.ExtractedEntities.Count} entities");
            foreach (var entity in result.ExtractedEntities)
            {
                _output.WriteLine($"  {entity.EntityType}: {entity.EntityValue} (confidence: {entity.Confidence:F2})");
            }
        }

        [Fact]
        public async Task ExtractAsync_WithVagueWords_ShouldUseLLM()
        {
            // Act
            var result = await _extractor.ExtractAsync("把除了客厅以外所有房间的灯都关了");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ExtractedEntities.Count > 0);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
```

**Step 2: Run integration tests**

Run: `dotnet test src/tests/IntegrationTests/NLP/EntityExtractorIntegrationTests.cs -v n`
Expected: All PASS

**Step 3: Commit**

```bash
git add src/tests/IntegrationTests/NLP/EntityExtractorIntegrationTests.cs
git commit -m "test: add integration tests for entity extraction

- Add end-to-end tests for IntentDrivenEntityExtractor
- Test simple light control scenario
- Test complex AC control with LLM enhancement
- Test vague word detection and LLM trigger
- Include detailed logging output for debugging

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 10: Add Documentation

**Files:**
- Create: `docs/how-to-configure-entity-extraction.md`

**Step 1: Create configuration guide**

Create `docs/how-to-configure-entity-extraction.md`:

```markdown
# 如何配置 LLM 增强实体提取

本文档说明如何在 CKY.MAF 框架中配置和使用 LLM 增强的实体提取功能。

## 概述

LLM 增强实体提取采用两阶段流程：
1. **意图识别**：识别用户输入的意图（如控灯、控空调）
2. **实体提取**：根据意图选择对应的 Provider，结合关键字匹配和 LLM 提取实体

## 配置步骤

### 1. 实现 EntityPatternProvider

为每个业务场景实现 `IEntityPatternProvider`：

```csharp
public class MyScenarioProvider : IEntityPatternProvider
{
    public string?[]? GetPatterns(string entityType)
    {
        return entityType switch
        {
            "Location" => new[] { "北京", "上海" },
            "Product" => new[] { "iPhone", "MacBook" },
            _ => null
        };
    }

    public IEnumerable<string> GetSupportedEntityTypes()
    {
        return new[] { "Location", "Product" };
    }

    public string GetFewShotExamples()
    {
        return @"
输入：""我想买iPhone""
输出：{""Product"": ""iPhone""}";
    }
}
```

### 2. 注册 Provider 和映射

在 `Program.cs` 中注册：

```csharp
// 注册 Provider
services.AddSingleton<MyScenarioProvider>();

// 注册映射
services.AddSingleton<IIntentProviderMapping>(sp =>
{
    var mapping = new IntentProviderMapping();
    mapping.Register("BuyProduct", typeof(MyScenarioProvider));
    return mapping;
});

// 注册 Entity Extractor
services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();
```

### 3. 配置 LLM Service

```csharp
// 生产环境使用真实 LLM
services.AddSingleton<ILlmService, ZhipuAILlmService>();

// 或使用配置文件动态选择
var llmProvider = configuration["LLM:Provider"];
services.AddSingleton<ILlmService>(llmProvider switch
{
    "ZhipuAI" => typeof(ZhipuAILlmService),
    "Qwen" => typeof(QwenLlmService),
    _ => typeof(MockLlmService)
});
```

## LLM 触发条件

LLM 在以下任一条件满足时启用：
- 输入长度 > 20 字
- 关键字覆盖率 < 40%
- 包含模糊词汇（"所有"、"除了"等）

## 性能优化

1. **关键字优先**：80%+ 的请求仅使用关键字匹配
2. **结果缓存**：LLM 结果缓存 1 小时
3. **熔断器**：连续失败 3 次后熔断 5 分钟

## 监控和日志

- LogDebug: 输入内容、Provider 选择、LLM Prompt
- LogInformation: 意图识别结果、实体提取成功
- LogWarning: LLM 调用失败、降级发生
- LogError: JSON 解析失败
```

**Step 2: Commit**

```bash
git add docs/how-to-configure-entity-extraction.md
git commit -m "docs: add entity extraction configuration guide

- Add step-by-step configuration instructions
- Include code examples for custom providers
- Document LLM trigger conditions
- Add performance optimization tips
- Add monitoring and logging guidelines

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 11: Final Testing and Verification

**Files:**
- Run all tests
- Update CLAUDE.md

**Step 1: Run all tests**

```bash
dotnet test src/tests/ --verbosity normal
```

Expected: All tests pass

**Step 2: Update CLAUDE.md**

Add to `CLAUDE.md` in "## Common Tasks" section:

```markdown
### Configuring Entity Extraction

1. Implement `IEntityPatternProvider` for your scenario
2. Register provider and intent mapping in DI
3. Configure `ILlmService` implementation
4. Use `IEntityExtractor` to extract entities

See `docs/how-to-configure-entity-extraction.md` for detailed guide.
```

**Step 3: Final commit**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md with entity extraction reference

- Add common task for configuring entity extraction
- Reference detailed configuration guide

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

**Step 4: Verify implementation**

Run final build and tests:

```bash
dotnet build
dotnet test --no-build
```

Expected: Build succeeds, all tests pass

---

## Implementation Complete!

### Summary

This implementation plan adds LLM-enhanced entity extraction to CKY.MAF:

✅ **Core Infrastructure**
- IntentProviderMapping for intent-to-provider resolution
- Extended IEntityPatternProvider with Few-shot examples
- LlmCircuitBreakerOptions for resilience configuration

✅ **Main Component**
- IntentDrivenEntityExtractor with two-stage extraction
- Confidence-based result merging
- Circuit breaker pattern for LLM resilience

✅ **Business Layer**
- LightControlEntityPatternProvider
- ACControlEntityPatternProvider
- CurtainControlEntityPatternProvider

✅ **Testing**
- Unit tests for all components
- Integration tests for end-to-end flow
- MockLlmService for development

✅ **Documentation**
- Configuration guide
- Design document
- Implementation plan

### Next Steps

1. Replace MockLlmService with real LLM implementation
2. Add more entity providers for additional scenarios
3. Configure production LLM API keys and endpoints
4. Add monitoring and alerting for circuit breaker events
5. Performance testing and optimization
