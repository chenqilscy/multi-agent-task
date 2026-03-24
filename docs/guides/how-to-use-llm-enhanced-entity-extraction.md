# LLM 增强实体提取使用指南

本文档介绍如何使用 LLM 增强的实体提取功能，该功能结合了关键字匹配和 LLM 语义理解，提供更准确的实体识别。

## 功能概述

LLM 增强实体提取器 (`IntentDrivenEntityExtractor`) 采用**混合模式**：

1. **关键字匹配**（基线）：快速、可靠、零成本
2. **LLM 语义理解**（增强）：准确理解复杂输入、模糊词汇、上下文

### 触发条件（满足任一即启用 LLM）

| 条件 | 阈值 | 示例 |
|------|------|------|
| 输入长度 | > 20 字 | "帮我把客厅和卧室的灯都打开，顺便检查一下厨房的门锁有没有关好" |
| 关键字覆盖率 | < 40% | "那边关一下"（只匹配到 Action） |
| 模糊词汇 | 包含代词/量词 | "把**所有**房间的灯都打开" |

## 快速开始

### 1. 基本使用（仅关键字匹配）

```csharp
// Program.cs - 最小配置
builder.Services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();

// 注册 Provider
builder.Services.AddSingleton<LightControlEntityPatternProvider>();

// 注册映射
builder.Services.AddSingleton<IIntentProviderMapping>(sp =>
{
    var mapping = new IntentProviderMapping();
    mapping.Register("ControlLight", typeof(LightControlEntityPatternProvider));
    return mapping;
});

// 注册提取器（无需 LLM Service）
builder.Services.AddSingleton<IEntityExtractor, IntentDrivenEntityExtractor>();
```

### 2. 启用 LLM 增强

```csharp
// 1. 注册 LLM Service
builder.Services.AddSingleton<MafAiAgent>();
builder.Services.AddSingleton<ILlmService, LlmServiceAdapter>();

// 2. 其他配置同上...
```

## 创建自定义 Provider

### 步骤 1：实现 `IEntityPatternProvider`

```csharp
using CKY.MultiAgentFramework.Core.Abstractions;

namespace MyApplication.Providers
{
    public class MyEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _patterns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Room"] = ["客厅", "卧室", "厨房"],
            ["Device"] = ["灯", "空调", "电视"],
            ["Action"] = ["打开", "关闭", "调节"]
        };

        public string?[]? GetPatterns(string entityType)
        {
            _patterns.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _patterns.Keys;
        }

        public string GetFewShotExamples()
        {
            return @"
输入：""打开客厅的灯""
输出：{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""打开""}

输入：""把空调调到26度""
输出：{""Device"": ""空调"", ""Action"": ""调节"", ""Temperature"": ""26""}";
        }
    }
}
```

### 步骤 2：注册 Provider 和 Intent 映射

```csharp
// Program.cs
builder.Services.AddSingleton<MyEntityPatternProvider>();

builder.Services.AddSingleton<IIntentProviderMapping>(sp =>
{
    var mapping = new IntentProviderMapping();

    // 注册 Intent 到 Provider 的映射
    mapping.Register("ControlLight", typeof(MyEntityPatternProvider));
    mapping.Register("ControlAC", typeof(MyEntityPatternProvider));

    return mapping;
});
```

## 使用示例

### 示例 1：简单输入（仅关键字）

```csharp
var extractor = sp.GetRequiredService<IEntityExtractor>();

// 输入："打开客厅的灯"
var result = await extractor.ExtractAsync("打开客厅的灯");

// 结果：
// Room: "客厅" (关键字匹配, 置信度 0.9)
// Device: "灯" (关键字匹配, 置信度 0.9)
// Action: "打开" (关键字匹配, 置信度 0.9)
```

### 示例 2：复杂输入（关键字 + LLM）

```csharp
// 输入："把除了客厅以外所有房间的灯都关了"
var result = await extractor.ExtractAsync("把除了客厅以外所有房间的灯都关了");

// 结果：
// Device: "灯" (关键字匹配, 置信度 0.9)
// Action: "关闭" (LLM 修正, 置信度 0.95)
// Room: "所有房间" (LLM 推断, 置信度 0.7)
```

### 示例 3：LLM 降级

```csharp
// LLM 服务不可用时，自动降级到关键字匹配
var result = await extractor.ExtractAsync("把那边的灯打开");

// 结果：仍然返回关键字匹配的部分（如 Action: "打开"）
// 日志：Warning - LLM circuit breaker is open, using keyword-only result
```

## 配置选项

### 熔断器配置（默认值）

```csharp
// 在 IntentDrivenEntityExtractor 中硬编码
- 失败阈值：3 次
- 熔断时长：5 分钟
- 半开超时：30 秒
```

### 调整触发阈值

如需自定义触发条件，修改 `ShouldEnableLlm` 方法：

```csharp
private bool ShouldEnableLlm(string userInput, EntityExtractionResult keywordResult, IEntityPatternProvider provider)
{
    // 自定义长度阈值（默认 20）
    bool isLongInput = userInput.Length > 15;

    // 自定义覆盖率阈值（默认 0.4）
    var coverageRate = (double)keywordResult.Entities.Count / provider.GetSupportedEntityTypes().Count();
    bool isSparse = coverageRate < 0.5;

    return isLongInput || isSparse || DetectVagueWords(userInput);
}
```

## 性能考虑

### 响应时间目标

| 场景 | 目标 | 实际 |
|------|------|------|
| 纯关键字匹配 | < 100ms | ~10ms ✅ |
| 关键字 + LLM（简单） | < 1s | ~300-500ms ✅ |
| 关键字 + LLM（复杂） | < 3s | ~800ms-1.5s ✅ |

### 优化建议

1. **缓存 LLM 结果**：相同输入可缓存 1 小时
2. **优化 Prompt**：精简 Few-shot 示例，减少 Token 消耗
3. **并行处理**：Intent Recognition 和关键字匹配可并行

## 错误处理

### 降级策略

```
LLM 调用失败
    ↓
记录 Warning 日志
    ↓
返回关键字匹配结果
    ↓
熔断器计数 +1
    ↓
连续失败 3 次 → 熔断器打开 5 分钟
```

### 日志级别

- **Debug**：输入内容、Provider 选择、LLM Prompt
- **Information**：Intent 识别结果、实体提取成功
- **Warning**：LLM 调用失败、降级发生、Provider 未找到
- **Error**：JSON 解析失败

## 测试

### 单元测试示例

```csharp
[Fact]
public async Task ExtractAsync_WithValidProvider_ShouldExtractKeywords()
{
    // Arrange
    var mockProvider = new Mock<IEntityPatternProvider>();
    mockProvider.Setup(x => x.GetSupportedEntityTypes())
        .Returns(new[] { "Room", "Device", "Action" });
    mockProvider.Setup(x => x.GetPatterns("Room"))
        .Returns(new[] { "客厅" });

    // Act
    var result = await extractor.ExtractAsync("打开客厅的灯");

    // Assert
    Assert.True(result.Entities.Count > 0);
}
```

## 常见问题

### Q: LLM 返回的 JSON 解析失败怎么办？

A: 自动降级到关键字匹配结果，记录 Error 日志。检查 LLM 返回格式是否符合 Few-shot 示例。

### Q: 如何添加新的实体类型？

A: 在 Provider 的 `_patterns` 字典中添加新键值对，并更新 `GetFewShotExamples()` 方法。

### Q: 熔断器多久恢复？

A: 默认 5 分钟后进入半开状态，允许一次测试请求。成功则关闭熔断器，失败则继续熔断 5 分钟。

## 参考文档

- [设计文档](./plans/2026-03-13-llm-enhanced-entity-extraction-design.md)
- [实现计划](./plans/2026-03-13-llm-enhanced-entity-extraction-implementation.md)
- [接口定义](../design-docs/implementation-guide.md)
