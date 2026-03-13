# LLM 增强实体提取器设计方案

**日期**: 2026-03-13
**作者**: Claude Code
**状态**: 设计完成，待实现

---

## 📋 目录

- [1. 设计目标](#1-设计目标)
- [2. 整体架构](#2-整体架构)
- [3. LLM 触发判断逻辑](#3-llm-触发判断逻辑)
- [4. Few-shot Prompt 设计](#4-few-shot-prompt-设计)
- [5. Intent 驱动的 Provider 选择](#5-intent-驱动的-provider-选择)
- [6. 置信度加权合并策略](#6-置信度加权合并策略)
- [7. 熔断器设计](#7-熔断器设计)
- [8. 错误处理和降级策略](#8-错误处理和降级策略)
- [9. 核心接口定义](#9-核心接口定义)
- [10. 测试策略](#10-测试策略)
- [11. 性能优化](#11-性能优化)
- [12. 实现注意事项](#12-实现注意事项)

---

## 1. 设计目标

### 问题背景

当前 `MafEntityExtractor` 仅使用关键字匹配提取实体，存在以下局限：
- ❌ 无法识别语义变体（如"开灯"vs"把灯打开"）
- ❌ 无法推断隐含实体（如"那边"需要推断具体房间）
- ❌ 无法处理数值实体（如"26度"中的数字）
- ❌ 对模糊词汇（如"所有"、"除了"）理解能力不足

### 设计目标

✅ **保持关键字匹配优势**：快速、稳定、低成本
✅ **LLM 作为补充增强**：提升语义理解能力
✅ **业务场景解耦**：框架层通用，业务层自定义
✅ **可观测性**：完整日志、监控、降级策略
✅ **性能可控**：80%+ 请求仅关键字匹配，<1s 响应

---

## 2. 整体架构

### 两阶段提取流程

```
┌─────────────────────────────────────────────────────────┐
│  Stage 1: Intent Recognition (意图识别)                 │
│  ┌───────────────────────────────────────────────────┐  │
│  │ 用户输入："把客厅空调调到26度"                    │  │
│  ↓                                                   │  │
│  │ IIntentRecognizer → 识别意图                      │  │
│  │ 结果：PrimaryIntent = "ControlAC" (控空调)         │  │
│  └───────────────────────────────────────────────────┘  │
│                       ↓                                  │
│  Stage 2: Entity Extraction (实体提取)                   │
│  ┌───────────────────────────────────────────────────┐  │
│  │ 根据 Intent → 选择对应的 Provider                  │  │
│  │ "ControlAC" → ACControlEntityPatternProvider       │  │
│  │                                                     │  │
│  │ 2.1 关键字匹配（始终执行）                          │  │
│  │     结果：{Device: 空调, Action: 调节}             │  │
│  │                                                     │  │
│  │ 2.2 判断是否启用 LLM                               │  │
│  │     • 长度 > 20 字?  YES                           │  │
│  │     • 覆盖率 < 40%?   YES                          │  │
│  │     • 包含模糊词?   NO                             │  │
│  │     → 启用 LLM                                     │  │
│  │                                                     │  │
│  │ 2.3 LLM 提取（熔断器保护）                          │  │
│  │     结果：{Room: 客厅, Temperature: 26}            │  │
│  │                                                     │  │
│  │ 2.4 置信度加权合并                                  │  │
│  │     最终：{                                        │  │
│  │       Room: 客厅 (LLM, 0.7)                        │  │
│  │       Device: 空调 (关键字, 0.9)                   │  │
│  │       Temperature: 26 (LLM, 0.95)                  │  │
│  │       Action: 调节 (关键字, 0.9)                   │  │
│  │     }                                              │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 核心类设计

```csharp
/// <summary>
/// 意图驱动的实体提取器
/// 根据 Intent 识别结果选择对应的 EntityPatternProvider
/// </summary>
public class IntentDrivenEntityExtractor : IEntityExtractor
{
    // Dependencies
    private readonly IIntentRecognizer _intentRecognizer;
    private readonly IIntentProviderMapping _mapping;
    private readonly ILlmService _llmService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAsyncPolicy _circuitBreakerPolicy;
    private readonly ILogger<IntentDrivenEntityExtractor> _logger;

    // Main Method
    public async Task<EntityExtractionResult> ExtractAsync(
        string userInput,
        CancellationToken ct = default);
}
```

---

## 3. LLM 触发判断逻辑

### 判断条件（满足任一即启用 LLM）

```csharp
private bool ShouldEnableLlm(string userInput, EntityExtractionResult keywordResult)
{
    // A. 长度阈值：> 20 字
    bool isLongInput = userInput.Length > 20;

    // B. 关键字稀疏度：< 40% 覆盖率
    var supportedTypes = _patternProvider.GetSupportedEntityTypes();
    double coverageRate = (double)keywordResult.Entities.Count / supportedTypes.Count();
    bool isSparse = coverageRate < 0.4;

    // C. 包含模糊词汇
    bool hasVagueWords = DetectVagueWords(userInput);

    return isLongInput || isSparse || hasVagueWords;
}
```

### 模糊词检测

```csharp
private bool DetectVagueWords(string input)
{
    // 模糊词库（可配置）
    var vagueWords = new[] {
        "那边", "所有", "全部", "除了", "以外",
        "那个", "这个", "它们", "大家", "各个"
    };

    return vagueWords.Any(word => input.Contains(word));
}
```

### 场景示例

| 输入 | 长度 | 关键字覆盖率 | 模糊词 | 启用 LLM? |
|------|------|--------------|--------|-----------|
| "打开灯" | 4 | 50% | ❌ | ❌ |
| "帮我把客厅的灯打开" | 11 | 75% | ❌ | ❌ |
| "把除了客厅以外所有房间的灯都关了" | 17 | 25% | ✅ | ✅ |
| "帮我检查一下那个房间的设备有没有问题" | 18 | 25% | ✅ | ✅ |
| "我想让系统自动把客厅和卧室的空调调到26度" | 23 | 50% | ❌ | ✅ (长度触发) |

---

## 4. Few-shot Prompt 设计

### Prompt 模板

```csharp
private string BuildLlmPrompt(string userInput, IEntityPatternProvider provider)
{
    var supportedTypes = provider.GetSupportedEntityTypes();
    var examples = provider.GetFewShotExamples();

    return $@"你是实体提取助手。请从用户输入中提取以下类型的实体：
{string.Join(", ", supportedTypes)}

提取规则：
1. 严格按照指定的实体类型提取
2. 如果用户输入中不存在某类实体，该字段不返回
3. 数值实体请提取原始数字（不含单位）
4. 置信度：0.0-1.0，表示提取的确信程度

示例：
{examples}

用户输入：{userInput}

请以JSON格式返回提取结果，包含 confidence 字段：
{{""EntityType"": ""Value"", ""confidence"": 0.95}}";
}
```

### 业务层提供示例（智能家居）

```csharp
// LightControlEntityPatternProvider
public string GetFewShotExamples()
{
    return @"
输入：""打开客厅的灯""
输出：{{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""打开""}}

输入：""把卧室灯调暗一点""
输出：{{""Room"": ""卧室"", ""Device"": ""灯"", ""Action"": ""调节"", ""Brightness"": ""降低""}}";
}

// ACControlEntityPatternProvider
public string GetFewShotExamples()
{
    return @"
输入：""把卧室空调调到制冷26度""
输出：{{""Room"": ""卧室"", ""Device"": ""空调"", ""Action"": ""调节"", ""Temperature"": ""26"", ""Mode"": ""制冷""}}

输入：""打开客厅空调""
输出：{{""Room"": ""客厅"", ""Device"": ""空调"", ""Action"": ""打开""}}";
}
```

---

## 5. Intent 驱动的 Provider 选择

### 核心思想

**Intent 和 EntityPatternProvider 的映射关系由业务层注册**

框架层提供映射机制，业务层决定：
- 有多少子场景（控灯、控空调、控窗帘...）
- 每个场景的实体类型
- Intent 到 Provider 的映射关系

### 框架层：映射接口

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

    /// <summary>
    /// 映射接口的默认实现
    /// </summary>
    public class IntentProviderMapping : IIntentProviderMapping
    {
        private readonly Dictionary<string, Type> _mapping = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string intent, Type providerType)
        {
            if (string.IsNullOrWhiteSpace(intent))
                throw new ArgumentException("Intent cannot be empty", nameof(intent));

            if (!typeof(IEntityPatternProvider).IsAssignableFrom(providerType))
                throw new ArgumentException($"Type must implement IEntityPatternProvider: {providerType.Name}");

            _mapping[intent] = providerType;
        }

        public Type? GetProviderType(string intent)
        {
            return _mapping.TryGetValue(intent, out var type) ? type : null;
        }

        public IEnumerable<string> GetRegisteredIntents()
        {
            return _mapping.Keys;
        }
    }
}
```

### 业务层：注册映射（Demo Program.cs）

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 1. 注册所有 Provider
        services.AddSingleton<LightControlEntityPatternProvider>();
        services.AddSingleton<ACControlEntityPatternProvider>();
        services.AddSingleton<CurtainControlEntityPatternProvider>();

        // 2. 注册 Intent → Provider 映射
        services.AddSingleton<IIntentProviderMapping>(sp =>
        {
            var mapping = new IntentProviderMapping();

            // 控灯场景
            mapping.Register("ControlLight", typeof(LightControlEntityPatternProvider));
            mapping.Register("DimLight", typeof(LightControlEntityPatternProvider));

            // 控空调场景
            mapping.Register("ControlAC", typeof(ACControlEntityPatternProvider));
            mapping.Register("SetTemperature", typeof(ACControlEntityPatternProvider));

            // 控窗帘场景
            mapping.Register("ControlCurtain", typeof(CurtainControlEntityPatternProvider));

            return mapping;
        });

        // 3. 注册其他服务
        services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();
        services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();
    }
}
```

### Provider 解析逻辑

```csharp
private IEntityPatternProvider? ResolveProvider(string intent)
{
    var providerType = _mapping.GetProviderType(intent);
    if (providerType == null)
    {
        _logger.LogWarning("No provider mapped for intent: {Intent}", intent);
        return null;
    }

    var provider = _serviceProvider.GetService(providerType) as IEntityPatternProvider;
    if (provider == null)
    {
        _logger.LogError("Provider type registered but not in DI: {TypeName}", providerType.Name);
        return null;
    }

    return provider;
}
```

---

## 6. 置信度加权合并策略

### 合并逻辑

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
```

### 场景示例

```
输入："把那边空调打开"

关键字匹配结果：
- Device: "空调" (Confidence: 0.9)
- Action: "打开" (Confidence: 0.9)

LLM 结果：
- Room: "客厅" (Confidence: 0.7)  ← 推断
- Device: "空调" (Confidence: 0.95)
- Action: "打开" (Confidence: 0.95)

合并后最终结果：
- Room: "客厅" (LLM 补充)
- Device: "空调" (LLM，0.95 > 0.9)
- Action: "打开" (LLM，0.95 > 0.9)
```

---

## 7. 熔断器设计

### 熔断器配置

```csharp
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
```

### 熔断器状态

```csharp
public enum CircuitState
{
    /// <summary>
    /// 正常状态：允许请求通过
    /// </summary>
    Closed,

    /// <summary>
    /// 熔断状态：拒绝请求，直接降级
    /// </summary>
    Open,

    /// <summary>
    /// 半开状态：允许一次测试请求，成功则恢复，失败则继续熔断
    /// </summary>
    HalfOpen
}
```

### Polly 集成

```csharp
public class IntentDrivenEntityExtractor : IEntityExtractor
{
    private readonly IAsyncPolicy _circuitBreakerPolicy;

    public IntentDrivenEntityExtractor(...)
    {
        // 创建熔断器策略
        _circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(5),
                onBreak: (ex, breakDelay) =>
                {
                    _logger.LogWarning(ex,
                        "LLM circuit breaker opened for {Delay}s due to: {Message}",
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

    public async Task<EntityExtractionResult> ExtractAsync(...)
    {
        // 关键字匹配（始终执行）
        var keywordResult = await ExtractByKeywordsAsync(...);

        // 判断是否需要 LLM
        if (!ShouldEnableLlm(userInput, keywordResult))
        {
            return keywordResult;
        }

        // 通过熔断器调用 LLM
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
            // 熔断器打开，降级到关键字结果
            _logger.LogWarning("LLM circuit breaker is open, using keyword-only result");
            return keywordResult;
        }
        catch (Exception ex)
        {
            // 其他异常，记录后降级
            _logger.LogError(ex, "LLM extraction failed, using keyword-only result");
            return keywordResult;
        }
    }
}
```

---

## 8. 错误处理和降级策略

### 降级策略总结

| 错误场景 | 降级策略 | 日志级别 | 用户体验 |
|----------|----------|----------|----------|
| LLM 调用失败（网络/超时） | 使用关键字结果 | Warning | 正常，功能略有降级 |
| LLM 熔断器打开 | 使用关键字结果 | Warning | 正常，LLM 暂时禁用 |
| Provider 未找到 | 返回空结果 | Warning | 无法提取实体 |
| JSON 解析失败 | 使用关键字结果 | Error | 正常 |
| Intent 识别失败 | 返回空结果 | Error | 无法提取实体 |

### 错误处理示例

```csharp
public async Task<EntityExtractionResult> ExtractAsync(
    string userInput,
    CancellationToken ct = default)
{
    try
    {
        // Stage 1: 识别意图
        IntentRecognitionResult intentResult;
        try
        {
            intentResult = await _intentRecognizer.RecognizeAsync(userInput, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Intent recognition failed for input: {Input}", userInput);
            return new EntityExtractionResult(); // 返回空结果
        }

        // Stage 2: 解析 Provider
        var provider = ResolveProvider(intentResult.PrimaryIntent);
        if (provider == null)
        {
            _logger.LogWarning("No provider found for intent: {Intent}", intentResult.PrimaryIntent);
            return new EntityExtractionResult(); // 返回空结果
        }

        // Stage 3: 关键字匹配
        var keywordResult = await ExtractByKeywordsAsync(userInput, provider, ct);

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
```

---

## 9. 核心接口定义

### 扩展 IEntityPatternProvider

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 实体模式提供者接口
    /// 用于解耦框架层与具体业务场景的实体模式定义
    /// </summary>
    public interface IEntityPatternProvider
    {
        /// <summary>
        /// 获取指定实体类型的关键字模式
        /// </summary>
        string?[]? GetPatterns(string entityType);

        /// <summary>
        /// 获取所有支持的实体类型
        /// </summary>
        IEnumerable<string> GetSupportedEntityTypes();

        /// <summary>
        /// 获取 Few-shot 示例（用于 LLM Prompt）
        /// 由业务层实现，提供该场景的示例
        /// </summary>
        string GetFewShotExamples();
    }
}
```

### 新增 IIntentProviderMapping

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

### IntentDrivenEntityExtractor 类

```csharp
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
            // 实现见前文"错误处理和降级策略"章节
        }

        // 私有辅助方法...
        private IEntityPatternProvider? ResolveProvider(string intent) { }
        private Task<EntityExtractionResult> ExtractByKeywordsAsync(...) { }
        private bool ShouldEnableLlm(string userInput, EntityExtractionResult keywordResult) { }
        private bool DetectVagueWords(string input) { }
        private Task<EntityExtractionResult> ExtractByLlmAsync(...) { }
        private EntityExtractionResult MergeResults(...) { }
        private string BuildLlmPrompt(...) { }
        private EntityExtractionResult ParseLlmResponse(...) { }
    }
}
```

---

## 10. 测试策略

### 测试金字塔

```
         ┌─────────────┐
         │  E2E Tests  │  5%  - 完整流程 + 真实 LLM
         ├─────────────┤
         │ Integration │  25% - Provider 映射、熔断器
         │   Tests     │       - Mock LLM，Testcontainers
         ├─────────────┤
         │  Unit Tests │  70% - 独立测试每个组件
         └─────────────┘       - Mock 所有依赖
```

### 单元测试（70%）

**测试重点**：
- ✅ LLM 触发判断逻辑（长度、稀疏度、模糊词）
- ✅ Provider 解析和映射
- ✅ 置信度加权合并逻辑
- ✅ Few-shot Prompt 生成

**示例**：

```csharp
public class IntentDrivenEntityExtractorTests
{
    [Fact]
    public async Task ExtractAsync_WhenIntentRecognized_ShouldUseMappedProvider()
    {
        // Arrange
        var mockIntentRecognizer = new Mock<IIntentRecognizer>();
        var mockMapping = new Mock<IIntentProviderMapping>();
        var mockProvider = new Mock<IEntityPatternProvider>();
        var mockLlmService = new Mock<ILlmService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

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
            .Returns(new[] { "Room", "Device", "Action" });

        // Act
        var extractor = new IntentDrivenEntityExtractor(
            mockIntentRecognizer.Object,
            mockMapping.Object,
            mockLlmService.Object,
            mockServiceProvider.Object,
            Mock.Of<ILogger<IntentDrivenEntityExtractor>>());

        var result = await extractor.ExtractAsync("把空调打开");

        // Assert
        mockProvider.Verify(x => x.GetPatterns(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("打开灯", false)]  // 短输入，不启用 LLM
    [InlineData("帮我把客厅和卧室的灯都打开，顺便检查一下厨房的门锁有没有关好", true)]
    public async Task ShouldEnableLlm_WithDifferentInputs_ShouldReturnExpected(
        string input, bool expected)
    {
        // Arrange
        var mockProvider = new Mock<IEntityPatternProvider>();
        mockProvider
            .Setup(x => x.GetSupportedEntityTypes())
            .Returns(new[] { "Room", "Device", "Action", "Value" });

        var extractor = CreateExtractor();

        // Act
        var keywordResult = await extractor.ExtractByKeywordsAsync(input, mockProvider.Object);
        var shouldEnable = extractor.ShouldEnableLlm(input, keywordResult);

        // Assert
        Assert.Equal(expected, shouldEnable);
    }
}
```

### 集成测试（25%）

**测试重点**：
- ✅ Intent → Provider 映射注册和解析
- ✅ 熔断器状态转换
- ✅ LLM 调用和降级逻辑
- ✅ 端到端流程（使用 Mock LLM）

**示例**：

```csharp
[Collection("Integration Tests")]
public class EntityExtractorIntegrationTests
{
    [Fact]
    public async Task ExtractAsync_WithMockLlm_ShouldReturnMergedResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Mock LLM Service
        var mockLlmService = new Mock<ILlmService>();
        mockLlmService
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{""Room"": ""客厅"", ""Device"": ""空调"", ""Temperature"": ""26""}");

        services.AddSingleton(mockLlmService.Object);

        // 注册映射
        services.AddSingleton<IIntentProviderMapping>(sp =>
        {
            var mapping = new IntentProviderMapping();
            mapping.Register("ControlAC", typeof(ACControlEntityPatternProvider));
            return mapping;
        });

        services.AddSingleton<IEntityPatternProvider, ACControlEntityPatternProvider>();
        services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();
        services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();

        var provider = services.BuildServiceProvider();
        var extractor = provider.GetRequiredService<IEntityExtractor>();

        // Act
        var result = await extractor.ExtractAsync("把客厅空调调到26度");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Entities.ContainsKey("Room"));
        Assert.Equal("客厅", result.Entities["Room"]);
    }
}
```

### E2E 测试（5%）

**测试重点**：
- ✅ 真实 LLM API 调用
- ✅ 完整的用户场景
- ✅ 性能和响应时间

**注意**：仅在测试环境运行，使用测试 API Key

---

## 11. 性能优化

### 目标指标

| 场景 | 目标响应时间 | 实际预估 |
|------|--------------|----------|
| 纯关键字匹配 | < 100ms | ✅ ~10ms |
| 关键字 + LLM（简单） | < 1s | ⚡ ~300-500ms |
| 关键字 + LLM（复杂） | < 3s | ⚡ ~800ms-1.5s |

### 优化策略

#### 1. 关键字优先

- 80%+ 的简单请求仅使用关键字匹配
- LLM 只处理复杂输入（长输入、稀疏输入、模糊词汇）

#### 2. Prompt 优化

```csharp
// 精简 Few-shot 示例，减少 Token 消耗
public string GetFewShotExamples()
{
    return @"
输入:""打开客厅灯""
输出:{""Room"":""客厅"",""Device"":""灯"",""Action"":""打开""}";
}
```

#### 3. 结果缓存

```csharp
public class CachedEntityExtractor : IEntityExtractor
{
    private readonly IEntityExtractor _innerExtractor;
    private readonly IMemoryCache _cache;

    public async Task<EntityExtractionResult> ExtractAsync(string userInput, CancellationToken ct)
    {
        var cacheKey = $"entity_extract:{ComputeHash(userInput)}";

        if (_cache.TryGetValue(cacheKey, out EntityExtractionResult? cached))
        {
            _logger.LogDebug("Cache hit for input: {Input}", userInput);
            return cached!;
        }

        var result = await _innerExtractor.ExtractAsync(userInput, ct);

        // 只缓存包含 LLM 结果的请求（关键字匹配太快，不需要缓存）
        if (result.ExtractedEntities.Any(e => e.Confidence < 0.9))
        {
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
        }

        return result;
    }

    private string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

#### 4. 并行处理（可选优化）

```csharp
// Intent Recognition 和关键字匹配可并行执行
public async Task<EntityExtractionResult> ExtractAsync(string userInput, CancellationToken ct)
{
    // 并行执行
    var intentTask = _intentRecognizer.RecognizeAsync(userInput, ct);
    var keywordTask = ExtractByKeywordsAsync(userInput, /* provider */ , ct);

    await Task.WhenAll(intentTask, keywordTask);

    var intentResult = await intentTask;
    var keywordResult = await keywordTask;

    // 后续处理...
}
```

---

## 12. 实现注意事项

### 12.1 依赖注入注册顺序

Demo Program.cs 中的正确注册顺序：

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 1. 先注册 LLM Service
        services.AddSingleton<ILlmService, ZhipuAILlmService>();

        // 2. 注册所有 Provider
        services.AddSingleton<LightControlEntityPatternProvider>();
        services.AddSingleton<ACControlEntityPatternProvider>();
        services.AddSingleton<CurtainControlEntityPatternProvider>();

        // 3. 注册 Intent → Provider 映射
        services.AddSingleton<IIntentProviderMapping>(sp =>
        {
            var mapping = new IntentProviderMapping();
            mapping.Register("ControlLight", typeof(LightControlEntityPatternProvider));
            mapping.Register("ControlAC", typeof(ACControlEntityPatternProvider));
            mapping.Register("ControlCurtain", typeof(CurtainControlEntityPatternProvider));
            return mapping;
        });

        // 4. 注册 Intent Recognizer
        services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();

        // 5. 注册 Entity Extractor
        services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();

        // 6. 可选：添加缓存装饰器
        services.Decorate<IEntityExtractor, CachedEntityExtractor>();
    }
}
```

### 12.2 错误处理要点

| 错误场景 | 处理方式 | 日志级别 |
|----------|----------|----------|
| LLM 调用失败（网络/超时） | 降级到关键字结果 | Warning |
| Provider 未找到 | 返回空结果 | Warning |
| 熔断器打开 | 使用关键字结果 | Warning |
| JSON 解析失败 | 使用关键字结果 | Error |
| Intent 识别失败 | 返回空结果 | Error |
| 未处理异常 | 抛出 | Critical |

### 12.3 日志记录规范

```csharp
// LogLevel 选择
- LogDebug: 输入内容、Provider 选择、LLM Prompt
- LogInformation: Intent 识别结果、实体提取成功
- LogWarning: LLM 调用失败、降级发生、Provider 未找到、熔断器状态变化
- LogError: JSON 解析失败、已知异常
- LogCritical: 未处理的严重异常
```

### 12.4 配置项

```json
// appsettings.json
{
  "EntityExtraction": {
    "LlmTriggerThresholds": {
      "MinLength": 20,
      "MinCoverageRate": 0.4
    },
    "CircuitBreaker": {
      "FailureThreshold": 3,
      "BreakDurationSeconds": 300,
      "HalfOpenTimeoutSeconds": 30
    },
    "Cache": {
      "Enabled": true,
      "TtlHours": 1
    },
    "VagueWords": [
      "那边", "所有", "全部", "除了", "以外",
      "那个", "这个", "它们", "大家", "各个"
    ]
  }
}
```

### 12.5 业务层实现示例

**LightControlEntityPatternProvider**（控灯场景）：

```csharp
namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    public class LightControlEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public LightControlEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = new[] { "客厅", "卧室", "厨房", "浴室", "书房", "餐厅", "阳台" },
                ["Device"] = new[] { "灯", "电灯", "照明", "吊灯", "台灯" },
                ["Action"] = new[] { "打开", "关闭", "调节", "设置", "开启", "关掉" },
                ["Brightness"] = new[] { "亮", "暗", "亮度" },
                ["Color"] = new[] { "红", "绿", "蓝", "白", "黄", "颜色" }
            };
        }

        public string?[]? GetPatterns(string entityType)
        {
            _entityPatternMap.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _entityPatternMap.Keys;
        }

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

---

## 📋 附录

### A. 相关文档

- [01-architecture-overview.md](../specs/01-architecture-overview.md) - 架构概览
- [06-interface-design-spec.md](../specs/06-interface-design-spec.md) - 接口设计规范
- [10-testing-guide.md](../specs/10-testing-guide.md) - 测试指南
- [13-performance-benchmarks.md](../specs/13-performance-benchmarks.md) - 性能基准
- [14-error-handling-guide.md](../specs/14-error-handling-guide.md) - 错误处理指南

### B. 依赖项

**NuGet 包**：
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Polly` (熔断器)
- `System.Text.Json` (JSON 解析)

**框架依赖**：
- Microsoft Agent Framework (Preview)

### C. 待实现功能清单

- [ ] 实现 `IntentProviderMapping` 类
- [ ] 实现 `IntentDrivenEntityExtractor` 类
- [ ] 扩展 `IEntityPatternProvider` 接口
- [ ] 实现业务层 Provider（LightControl, ACControl, CurtainControl）
- [ ] 添加单元测试
- [ ] 添加集成测试
- [ ] 添加缓存装饰器（可选）
- [ ] 配置化熔断器参数
- [ ] 性能测试和优化

---

**文档版本**: 1.0
**最后更新**: 2026-03-13
