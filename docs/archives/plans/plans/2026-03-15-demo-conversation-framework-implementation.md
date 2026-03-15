# Demo对话框架实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use @superpowers:executing-plans 或 @superpowers:subagent-driven-development 来实施此计划。

**目标:** 为SmartHome和CustomerService Demo构建共通的对话能力框架，包括槽位管理、澄清管理、指代消解、意图飘移检测等核心组件。

**架构:** 基于CKY.MAF的5层DIP架构，Phase 1实现Layer 4的共通业务服务层，包括5个新增组件和4个增强组件，所有组件遵循SOLID原则并通过接口抽象。

**技术栈:** .NET 10, Microsoft Agent Framework (Preview), xUnit, Moq, FluentAssertions

---

## 📋 任务清单总览

### Phase 1.1: 槽位管理器（2天）
- Task 1.1: 创建槽位数据模型
- Task 1.2: 实现ISlotManager接口
- Task 1.3: 实现预定义槽位模板检测
- Task 1.4: 实现LLM动态槽位识别
- Task 1.5: 实现槽位填充逻辑
- Task 1.6: 实现澄清问题生成
- Task 1.7: 单元测试

### Phase 1.2: 澄清管理器（1.5天）
- Task 2.1: 创建澄清数据模型
- Task 2.2: 实现IClarificationManager接口
- Task 2.3: 实现澄清策略选择
- Task 2.4: 实现模板澄清生成
- Task 2.5: 实现LLM澄清生成
- Task 2.6: 实现用户响应处理
- Task 2.7: 单元测试

### Phase 1.3: 增强现有组件（1.5天）
- Task 3.1: 增强MafCoreferenceResolver
- Task 3.2: 增强MafTaskDecomposer
- Task 3.3: 重构DialogueAgent
- Task 3.4: 集成测试

### Phase 1.4: 新增组件（2天）
- Task 4.1: 实现IntentDriftDetector
- Task 4.2: 实现DialogStateManager
- Task 4.3: 单元测试

### Phase 1.5: 集成与文档（1天）
- Task 5.1: E2E测试
- Task 5.2: 性能测试
- Task 5.3: API文档
- Task 5.4: 使用示例

---

## Phase 1.1: 槽位管理器（2天）

### Task 1.1: 创建槽位数据模型

**Files:**
- Create: `src/Core/Models/Dialog/SlotDefinition.cs`
- Create: `src/Core/Models/Dialog/IntentSlotDefinition.cs`
- Create: `src/Core/Models/Dialog/SlotDetectionResult.cs`

**Step 1: 创建SlotDefinition类**

```csharp
// src/Core/Models/Dialog/SlotDefinition.cs
namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    public enum SlotType
    {
        String,
        Integer,
        Float,
        Boolean,
        Enum,
        Date,
        Object,
        Array
    }

    public class SlotDefinition
    {
        public string SlotName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SlotType Type { get; set; }
        public bool Required { get; set; } = true;
        public bool HasDefaultValue { get; set; }
        public object? DefaultValue { get; set; }
        public List<string> Synonyms { get; set; } = new();
        public string[]? ValidValues { get; set; }
        public int DependencyLevel { get; set; }
    }
}
```

**Step 2: 创建IntentSlotDefinition类**

```csharp
// src/Core/Models/Dialog/IntentSlotDefinition.cs
namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    public class IntentSlotDefinition
    {
        public string Intent { get; set; } = string.Empty;
        public List<SlotDefinition> RequiredSlots { get; set; } = new();
        public List<SlotDefinition> OptionalSlots { get; set; } = new();
    }
}
```

**Step 3: 创建SlotDetectionResult类**

```csharp
// src/Core/Models/Dialog/SlotDetectionResult.cs
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    public class SlotDetectionResult
    {
        public string Intent { get; set; } = string.Empty;
        public List<SlotDefinition> MissingSlots { get; set; } = new();
        public List<SlotDefinition> OptionalSlots { get; set; } = new();
        public Dictionary<string, object> DetectedSlots { get; set; } = new();
        public double Confidence { get; set; }
    }
}
```

**Step 4: 运行构建验证**

```bash
cd src/Core
dotnet build
```

Expected: SUCCESS

**Step 5: Commit**

```bash
git add src/Core/Models/Dialog/
git commit -m "feat: add slot management data models"
```

---

### Task 1.2: 实现ISlotManager接口

**Files:**
- Create: `src/Core/Abstractions/ISlotManager.cs`
- Create: `src/Core/Abstractions/ISlotDefinitionProvider.cs`

**Step 1: 创建ISlotManager接口**

```csharp
// src/Core/Abstractions/ISlotManager.cs
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    public interface ISlotManager
    {
        Task<SlotDetectionResult> DetectMissingSlotsAsync(
            string userInput,
            IntentRecognitionResult intent,
            EntityExtractionResult entities,
            CancellationToken ct = default);

        Task<Dictionary<string, object>> FillSlotsAsync(
            string intent,
            Dictionary<string, object> providedSlots,
            DialogContext context,
            CancellationToken ct = default);

        Task<string> GenerateClarificationAsync(
            List<SlotDefinition> missingSlots,
            string intent,
            CancellationToken ct = default);
    }
}
```

**Step 2: 创建ISlotDefinitionProvider接口**

```csharp
// src/Core/Abstractions/ISlotDefinitionProvider.cs
using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    public interface ISlotDefinitionProvider
    {
        IntentSlotDefinition? GetDefinition(string intent);
        Dictionary<string, IntentSlotDefinition> GetAllDefinitions();
    }
}
```

**Step 3: 运行构建验证**

```bash
cd src/Core
dotnet build
```

**Step 4: Commit**

```bash
git add src/Core/Abstractions/
git commit -m "feat: add ISlotManager and ISlotDefinitionProvider interfaces"
```

---

### Task 1.3: 实现预定义槽位模板检测

**Files:**
- Create: `src/Services/Dialog/SlotManager.cs`
- Create: `src/Demos/SmartHome/Providers/SmartHomeSlotDefinitionProvider.cs`

**Step 1: 编写SlotManager测试（TDD - 先写测试）**

```csharp
// src/tests/UnitTests/Services/Dialog/SlotManagerTests.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Dialog;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.UnitTests.Services.Dialog
{
    public class SlotManagerTests
    {
        [Fact]
        public async Task DetectMissingSlots_WithPredefinedIntent_ReturnsCorrectSlots()
        {
            // Arrange
            var mockProvider = new Mock<ISlotDefinitionProvider>();
            var slotDef = new IntentSlotDefinition
            {
                Intent = "control_device",
                RequiredSlots = new()
                {
                    new SlotDefinition { SlotName = "Device", Description = "设备名称" },
                    new SlotDefinition { SlotName = "Action", Description = "操作类型" },
                    new SlotDefinition { SlotName = "Location", Description = "位置" }
                }
            };
            mockProvider.Setup(p => p.GetDefinition("control_device")).Returns(slotDef);

            var mockLlm = new Mock<IMafAiAgentRegistry>();
            var logger = new Mock<ILogger<SlotManager>>().Object;

            var slotManager = new SlotManager(mockProvider.Object, mockLlm.Object, logger);

            var intent = new IntentRecognitionResult
            {
                PrimaryIntent = "control_device",
                Confidence = 0.9
            };
            var entities = new EntityExtractionResult
            {
                Entities = new Dictionary<string, object>
                {
                    ["Device"] = "空调",
                    ["Action"] = "打开"
                }
            };

            // Act
            var result = await slotManager.DetectMissingSlotsAsync(
                "打开空调", intent, entities, CancellationToken.None);

            // Assert
            Assert.Equal("control_device", result.Intent);
            Assert.Single(result.MissingSlots);
            Assert.Equal("Location", result.MissingSlots[0].SlotName);
            Assert.Equal(2, result.DetectedSlots.Count);
            Assert.Equal(0.67, result.Confidence, 0.01); // 2/3 slots filled
        }
    }
}
```

**Step 2: 运行测试验证失败**

```bash
cd src/tests
dotnet test --filter "SlotManagerTests"
```

Expected: FAIL (SlotManager not implemented)

**Step 3: 实现SlotManager基础结构**

```csharp
// src/Services/Dialog/SlotManager.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    public class SlotManager : ISlotManager
    {
        private readonly ISlotDefinitionProvider _slotDefinitionProvider;
        private readonly IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<SlotManager> _logger;

        public SlotManager(
            ISlotDefinitionProvider slotDefinitionProvider,
            IMafAiAgentRegistry llmRegistry,
            ILogger<SlotManager> logger)
        {
            _slotDefinitionProvider = slotDefinitionProvider ?? throw new ArgumentNullException(nameof(slotDefinitionProvider));
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SlotDetectionResult> DetectMissingSlotsAsync(
            string userInput,
            IntentRecognitionResult intent,
            EntityExtractionResult entities,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Detecting missing slots for intent: {Intent}", intent.PrimaryIntent);

            var slotDef = _slotDefinitionProvider.GetDefinition(intent.PrimaryIntent);
            if (slotDef == null)
            {
                _logger.LogWarning("No slot definition found for intent: {Intent}", intent.PrimaryIntent);
                return new SlotDetectionResult
                {
                    Intent = intent.PrimaryIntent,
                    Confidence = 0.0
                };
            }

            var missingSlots = new List<SlotDefinition>();
            var detectedSlots = new Dictionary<string, object>();

            // 检查必需槽位
            foreach (var requiredSlot in slotDef.RequiredSlots)
            {
                if (entities.Entities.ContainsKey(requiredSlot.SlotName))
                {
                    detectedSlots[requiredSlot.SlotName] = entities.Entities[requiredSlot.SlotName];
                }
                else
                {
                    missingSlots.Add(requiredSlot);
                }
            }

            var confidence = slotDef.RequiredSlots.Count == 0
                ? 1.0
                : (double)detectedSlots.Count / slotDef.RequiredSlots.Count;

            return new SlotDetectionResult
            {
                Intent = intent.PrimaryIntent,
                MissingSlots = missingSlots,
                DetectedSlots = detectedSlots,
                Confidence = confidence
            };
        }

        public Task<Dictionary<string, object>> FillSlotsAsync(
            string intent,
            Dictionary<string, object> providedSlots,
            DialogContext context,
            CancellationToken ct = default)
        {
            // TODO: Task 1.5 实现
            return Task.FromResult(providedSlots);
        }

        public Task<string> GenerateClarificationAsync(
            List<SlotDefinition> missingSlots,
            string intent,
            CancellationToken ct = default)
        {
            // TODO: Task 1.6 实现
            return Task.FromResult("请提供更多信息");
        }
    }
}
```

**Step 4: 实现SmartHomeSlotDefinitionProvider**

```csharp
// src/Demos/SmartHome/Providers/SmartHomeSlotDefinitionProvider.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Providers
{
    public class SmartHomeSlotDefinitionProvider : ISlotDefinitionProvider
    {
        private readonly Dictionary<string, IntentSlotDefinition> _definitions;

        public SmartHomeSlotDefinitionProvider()
        {
            _definitions = new Dictionary<string, IntentSlotDefinition>
            {
                ["control_device"] = new IntentSlotDefinition
                {
                    Intent = "control_device",
                    RequiredSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Device",
                            Description = "设备名称",
                            Type = SlotType.String
                        },
                        new SlotDefinition
                        {
                            SlotName = "Action",
                            Description = "操作类型",
                            Type = SlotType.Enum,
                            ValidValues = new[] { "打开", "关闭", "调节" }
                        },
                        new SlotDefinition
                        {
                            SlotName = "Location",
                            Description = "位置",
                            Type = SlotType.String
                        }
                    },
                    OptionalSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Mode",
                            Description = "模式",
                            Type = SlotType.Enum,
                            ValidValues = new[] { "制冷", "制热", "除湿", "送风" },
                            HasDefaultValue = true,
                            DefaultValue = "自动"
                        },
                        new SlotDefinition
                        {
                            SlotName = "Temperature",
                            Description = "温度",
                            Type = SlotType.Integer,
                            HasDefaultValue = true,
                            DefaultValue = 26
                        }
                    }
                },
                ["query_weather"] = new IntentSlotDefinition
                {
                    Intent = "query_weather",
                    RequiredSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Location",
                            Description = "城市",
                            Type = SlotType.String
                        }
                    },
                    OptionalSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Date",
                            Description = "日期",
                            Type = SlotType.Date,
                            HasDefaultValue = true,
                            DefaultValue = DateTime.Today
                        },
                        new SlotDefinition
                        {
                            SlotName = "TimeRange",
                            Description = "时间范围",
                            Type = SlotType.String,
                            ValidValues = new[] { "今天", "明天", "本周", "最近几天" }
                        }
                    }
                }
            };
        }

        public IntentSlotDefinition? GetDefinition(string intent)
        {
            return _definitions.GetValueOrDefault(intent);
        }

        public Dictionary<string, IntentSlotDefinition> GetAllDefinitions()
        {
            return _definitions;
        }
    }
}
```

**Step 5: 运行测试验证通过**

```bash
cd src/tests
dotnet test --filter "SlotManagerTests"
```

Expected: PASS

**Step 6: Commit**

```bash
git add src/Services/Dialog/ src/Demos/SmartHome/Providers/ src/tests/
git commit -m "feat: implement predefined slot detection with SlotManager"
```

---

### Task 1.4: 实现LLM动态槽位识别

**Files:**
- Modify: `src/Services/Dialog/SlotManager.cs`

**Step 1: 扩展SlotManager支持LLM识别**

```csharp
// 在SlotManager类的DetectMissingSlotsAsync方法中添加
public async Task<SlotDetectionResult> DetectMissingSlotsAsync(
    string userInput,
    IntentRecognitionResult intent,
    EntityExtractionResult entities,
    CancellationToken ct = default)
{
    _logger.LogDebug("Detecting missing slots for intent: {Intent}", intent.PrimaryIntent);

    var slotDef = _slotDefinitionProvider.GetDefinition(intent.PrimaryIntent);

    if (slotDef != null)
    {
        // 预定义意图 → 使用模板检测
        return await DetectWithTemplateAsync(slotDef, entities, ct);
    }

    // 未知意图 → 使用LLM动态识别
    return await DetectWithLlmAsync(userInput, intent.PrimaryIntent, entities, ct);
}

private async Task<SlotDetectionResult> DetectWithTemplateAsync(
    IntentSlotDefinition slotDef,
    EntityExtractionResult entities,
    CancellationToken ct)
{
    var missingSlots = new List<SlotDefinition>();
    var detectedSlots = new Dictionary<string, object>();

    // 检查必需槽位
    foreach (var requiredSlot in slotDef.RequiredSlots)
    {
        if (entities.Entities.ContainsKey(requiredSlot.SlotName))
        {
            detectedSlots[requiredSlot.SlotName] = entities.Entities[requiredSlot.SlotName];
        }
        else
        {
            missingSlots.Add(requiredSlot);
        }
    }

    var confidence = slotDef.RequiredSlots.Count == 0
        ? 1.0
        : (double)detectedSlots.Count / slotDef.RequiredSlots.Count;

    return new SlotDetectionResult
    {
        Intent = slotDef.Intent,
        MissingSlots = missingSlots,
        DetectedSlots = detectedSlots,
        Confidence = confidence
    };
}

private async Task<SlotDetectionResult> DetectWithLlmAsync(
    string userInput,
    string intent,
    EntityExtractionResult entities,
    CancellationToken ct)
{
    _logger.LogInformation("Using LLM to detect slots for unknown intent: {Intent}", intent);

    var prompt = $@"
分析用户请求，识别完成该意图所需的槽位信息：

用户输入：{userInput}
识别意图：{intent}

请分析：
1. 完成该意图需要哪些信息槽位（slots）？
2. 用户已提供了哪些槽位？
3. 缺失哪些槽位？

返回JSON格式：
{{
  ""required_slots"": [
    {{ ""name"": ""Location"", ""description"": ""城市"", ""provided"": false }},
    {{ ""name"": ""Date"", ""description"": ""日期"", ""provided"": true, ""value"": ""今天"" }}
  ],
  ""confidence"": 0.5
}}
";

    try
    {
        var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Intent, ct);
        var response = await llmAgent.ExecuteAsync(
            llmAgent.GetCurrentModelId(),
            prompt,
            null,
            ct);

        return ParseLlmSlotDetection(response, intent, entities);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "LLM slot detection failed");
        return new SlotDetectionResult
        {
            Intent = intent,
            Confidence = 0.0
        };
    }
}

private SlotDetectionResult ParseLlmSlotDetection(
    string llmResponse,
    string intent,
    EntityExtractionResult entities)
{
    // 解析LLM返回的JSON
    // TODO: 完善JSON解析逻辑
    return new SlotDetectionResult
    {
        Intent = intent,
        DetectedSlots = entities.Entities,
        Confidence = 0.5
    };
}
```

**Step 2: 添加单元测试**

```csharp
[Fact]
public async Task DetectMissingSlots_WithUnknownIntent_UsesLlm()
{
    // Arrange
    var mockProvider = new Mock<ISlotDefinitionProvider>();
    mockProvider.Setup(p => p.GetDefinition("unknown_intent")).Returns((IntentSlotDefinition?)null);

    var mockLlm = new Mock<IMafAiAgentRegistry>();
    var mockAgent = new Mock<MafAiAgent>();
    mockAgent.Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(@"{""required_slots"": []}");

    var mockRegistry = new Mock<IMafAiAgentRegistry>();
    mockRegistry.Setup(r => r.GetBestAgentAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockAgent.Object);

    var logger = new Mock<ILogger<SlotManager>>().Object;
    var slotManager = new SlotManager(mockProvider.Object, mockRegistry.Object, logger);

    var intent = new IntentRecognitionResult { PrimaryIntent = "unknown_intent" };
    var entities = new EntityExtractionResult { Entities = new Dictionary<string, object>() };

    // Act
    var result = await slotManager.DetectMissingSlotsAsync(
        "unknown request", intent, entities, CancellationToken.None);

    // Assert
    mockRegistry.Verify(r => r.GetBestAgentAsync(LlmScenario.Intent, It.IsAny<CancellationToken>()), Times.Once);
}
```

**Step 3: 运行测试**

```bash
dotnet test --filter "SlotManagerTests"
```

Expected: PASS

**Step 4: Commit**

```bash
git add src/Services/Dialog/ src/tests/
git commit -m "feat: add LLM-based slot detection for unknown intents"
```

---

### Task 1.5-1.7: 继续实现槽位填充、澄清生成和测试

由于篇幅限制，后续任务（1.5-1.7）的详细步骤将在实际实施时继续。每个任务遵循相同的TDD流程：
1. 编写测试
2. 运行测试验证失败
3. 实现最小功能
4. 运行测试验证通过
5. Commit

---

## Phase 1.2: 澄清管理器（1.5天）

### Task 2.1: 创建澄清数据模型

**Files:**
- Create: `src/Core/Models/Dialog/ClarificationModels.cs`

**实现代码：**

```csharp
namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    public enum ClarificationStrategy
    {
        Template,
        SmartInference,
        LLM,
        Hybrid
    }

    public class ClarificationAnalysis
    {
        public bool NeedsClarification { get; set; }
        public List<SlotDefinition> MissingSlots { get; set; } = new();
        public List<SlotDefinition> PrioritySlots { get; set; } = new();
        public ClarificationStrategy Strategy { get; set; }
        public double Confidence { get; set; }
        public int EstimatedTurns { get; set; }
        public DialogContext? Context { get; set; }
        public object? SuggestedValue { get; set; }
        public bool RequiresConfirmation { get; set; }
    }

    public class ClarificationContext
    {
        public string SessionId { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public List<SlotDefinition> MissingSlots { get; set; } = new();
        public int TurnCount { get; set; }
    }

    public class ClarificationResponse
    {
        public bool Completed { get; set; }
        public Dictionary<string, object> UpdatedSlots { get; set; } = new();
        public List<SlotDefinition> StillMissing { get; set; } = new();
        public string? Message { get; set; }
    }
}
```

---

## Phase 1.3-1.5: 后续任务概要

由于文档长度限制，完整的实施计划包含以下任务：

### Phase 1.3: 增强现有组件（1.5天）
- Task 3.1: 增强MafCoreferenceResolver（添加LLM消解能力）
- Task 3.2: 增强MafTaskDecomposer（添加LLM任务拆解）
- Task 3.3: 重构DialogueAgent为A2A Agent
- Task 3.4: 集成测试

### Phase 1.4: 新增组件（2天）
- Task 4.1: 实现IntentDriftDetector
- Task 4.2: 实现DialogStateManager
- Task 4.3: 单元测试

### Phase 1.5: 集成与文档（1天）
- Task 5.1: E2E测试（3个完整场景）
- Task 5.2: 性能测试（响应时间、并发）
- Task 5.3: API文档（XML注释）
- Task 5.4: 使用示例和README

---

## 实施原则

### TDD (测试驱动开发)
- 每个功能先编写测试
- 测试失败后再实现功能
- 保持红-绿-重构循环

### DRY (不要重复自己)
- 复用已有组件
- 提取公共逻辑到基类
- 使用组合优于继承

### YAGNI (你不会需要它)
- 只实现当前需要的功能
- 避免过度设计
- 保持简单可维护

### 频繁提交
- 每个Task完成后commit
- Commit message格式：`feat:`, `fix:`, `refactor:`, `test:`
- 保持commit历史清晰

---

## 验收标准

### 功能验收
- [ ] SmartHome Demo支持3个案例场景（天气查询、设备控制、复杂多轮对话）
- [ ] 支持槽位缺失检测与澄清
- [ ] 支持指代消解
- [ ] 支持意图飘移检测
- [ ] 支持多轮对话状态管理

### 质量验收
- [ ] 单元测试覆盖率 > 70%
- [ ] 集成测试覆盖核心流程
- [ ] E2E测试通过3个完整场景
- [ ] 所有公共API有XML注释

### 性能验收
- [ ] 简单任务响应 < 1s
- [ ] 复杂任务响应 < 5s
- [ ] 并发支持 > 100用户

---

## 附录

### A. 参考文档
- [设计文档](./2026-03-15-demo-conversation-framework-design.md)
- [CKY.MAF架构概览](../specs/01-architecture-overview.md)
- [接口设计规范](../specs/06-interface-design-spec.md)

### B. 依赖项
- .NET 10
- Microsoft Agent Framework (Preview)
- xUnit 2.6+
- Moq 4.20+
- FluentAssertions 6.12+

### C. 环境准备
```bash
# 还原NuGet包
dotnet restore

# 构建解决方案
dotnet build

# 运行所有测试
dotnet test
```

---

**文档维护**: CKY.MAF架构团队
**创建日期**: 2026-03-15
**预计完成**: 2026-03-22 (7天)
**审核状态**: 待审核
