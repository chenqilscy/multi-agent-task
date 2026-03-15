# 对话管理使用示例

本文档提供对话管理组件的使用示例和最佳实践。

## 目录

1. [槽位管理 (SlotManager)](#槽位管理-slotmanager)
2. [指代消解 (MafCoreferenceResolver)](#指代消解-mafcoreferenceresolver)
3. [意图漂移检测 (IntentDriftDetector)](#意图漂移检测-intentdriftdetector)
4. [对话状态管理 (DialogStateManager)](#对话状态管理-dialogstatemanager)
5. [完整示例：多轮对话流程](#完整示例多轮对话流程)
6. [集成测试示例](#集成测试示例)

## 槽位管理 (SlotManager)

### 基本用法

```csharp
// 注册服务（在 Program.cs）
services.AddSingleton<ISlotManager, SlotManager>();
services.AddSingleton<ISlotDefinitionProvider, SlotDefinitionProvider>();

// 使用槽位管理器
public class DeviceControlService
{
    private readonly ISlotManager _slotManager;

    public DeviceControlService(ISlotManager slotManager)
    {
        _slotManager = slotManager;
    }

    public async Task ControlDeviceAsync(string userInput)
    {
        // 1. 定义意图和槽位
        var intent = "control_device";
        var context = new DialogContext { SessionId = "session1" };

        // 2. 检测槽位
        var slots = await _slotManager.DetectSlotsAsync(intent, userInput);

        // 3. 检查缺失的槽位
        var missingSlots = _slotManager.GetMissingSlots(intent, slots);

        if (missingSlots.Count > 0)
        {
            // 4. 生成澄清问题
            var questions = _slotManager.GenerateClarificationQuestions(intent, missingSlots);
            return questions[0].Question; // "请问您想控制哪个位置的设备？"
        }

        // 5. 填充槽位
        await _slotManager.FillSlotAsync(context, intent, "device", "空调");
        await _slotManager.FillSlotAsync(context, intent, "location", "客厅");

        // 6. 获取已填充的槽位
        var filledSlots = await _slotManager.GetFilledSlotsAsync(context, intent);
        Console.WriteLine($"设备: {filledSlots["device"].Value}, 位置: {filledSlots["location"].Value}");
    }
}
```

### 预定义槽位

```csharp
// 定义槽位模板
var slotDefinition = new SlotDefinition
{
    IntentName = "control_device",
    RequiredSlots = new List<SlotTemplate>
    {
        new() { SlotName = "device", SlotType = SlotType.String, DisplayName = "设备" },
        new() { SlotName = "location", SlotType = SlotType.String, DisplayName = "位置" },
        new() { SlotName = "action", SlotType = SlotType.String, DisplayName = "操作" }
    }
};

// 从用户输入提取槽位值
var slots = await slotManager.DetectSlotsAsync("control_device", "打开客厅的空调");
// 结果: { device: "空调", location: "客厅", action: "打开" }
```

### 槽位验证

```csharp
// 自定义验证规则
public class TemperatureSlotValidator : ISlotValidator
{
    public (bool IsValid, string? ErrorMessage) Validate(string slotName, object value)
    {
        if (slotName == "temperature" && value is int temp)
        {
            if (temp < 16 || temp > 30)
            {
                return (false, "温度应在16-30度之间");
            }
        }
        return (true, null);
    }
}

// 注册验证器
services.AddSingleton<ISlotValidator, TemperatureSlotValidator>();
```

## 指代消解 (MafCoreferenceResolver)

### 基本用法

```csharp
public class DialogueService
{
    private readonly ICoreferenceResolver _coreferenceResolver;
    private readonly IMafSessionStorage _sessionStorage;

    public async Task ProcessMultiTurnDialog(string userInput, string conversationId)
    {
        // 第一轮: 用户说 "打开客厅的空调"
        await sessionStorage.AddMessageAsync(conversationId,
            new MafAiMessage { Role = "user", Content = "打开客厅的空调" });

        // 第二轮: 用户说 "把它调到26度"
        var resolved = await _coreferenceResolver.ResolveAsync(
            "把它调到26度",
            conversationId
        );

        // 结果: "把空调调到26度" (代词 "它" 被替换为 "空调")
        Console.WriteLine(resolved);
    }
}
```

### 使用LLM增强指代消解

```csharp
public async Task AdvancedCoreferenceResolution()
{
    var context = new DialogContext
    {
        SessionId = "session1",
        TurnCount = 3,
        PreviousIntent = "ControlLight",
        HistoricalSlots = new Dictionary<string, object>
        {
            ["control_device.device"] = "客厅灯",
            ["control_device.location"] = "客厅"
        }
    };

    var entities = new Dictionary<string, object>
    {
        ["device"] = "客厅灯",
        ["location"] = "客厅"
    };

    var userInput = "把它调亮一点";

    var resolver = new MafCoreferenceResolver(sessionStorage, logger);
    var resolved = await resolver.ResolveCoreferencesWithLlmAsync(
        userInput,
        context,
        entities
    );

    // 结果: "把客厅灯调亮一点"
}
```

## 意图漂移检测 (IntentDriftDetector)

### 检测话题转换

```csharp
public class ConversationManager
{
    private readonly IntentDriftDetector _driftDetector;

    public async Task HandleUserInput(string input, DialogContext context)
    {
        // 检测意图漂移
        var analysis = await _driftDetector.DetectDriftAsync(
            currentInput: input,
            previousIntent: context.PreviousIntent ?? "",
            context: context
        );

        if (analysis.HasDrifted)
        {
            Console.WriteLine($"检测到意图漂移: {analysis.Reason}");

            switch (analysis.SuggestedAction)
            {
                case DriftAction.NewTopic:
                    // 保存当前状态，开始新话题
                    await SaveCurrentStateAndStartNew();
                    break;

                case DriftAction.PossibleNewTopic:
                    // 询问用户确认
                    await AskUserForConfirmation();
                    break;

                case DriftAction.Continue:
                default:
                    // 继续当前话题
                    break;
            }
        }
    }

    private async Task SaveCurrentStateAndStartNew()
    {
        // 实现状态保存逻辑
    }
}
```

### 触发词检测

```csharp
// 检测话题转换触发词
var input = "对了，今天天气怎么样？";
var analysis = await driftDetector.DetectDriftAsync(input, "ControlLight", context);

// analysis.HasDrifted = true (检测到 "对了")
// analysis.SuggestedAction = DriftAction.NewTopic
```

### 语义相似度检测

```csharp
// 基于历史槽位判断语义相似度
var context = new DialogContext
{
    HistoricalSlots = new Dictionary<string, object>
    {
        ["control_device.device"] = "空调",
        ["control_device.location"] = "客厅"
    }
};

var input = "播放周杰伦的歌";
var analysis = await driftDetector.DetectDriftAsync(input, "ControlLight", context);

// analysis.SemanticSimilarityScore < 0.3
// analysis.HasDrifted = true (语义相似度低)
```

## 对话状态管理 (DialogStateManager)

### 基本用法

```csharp
public class DialogOrchestrator
{
    private readonly DialogStateManager _stateManager;

    public async Task HandleDialogTurn()
    {
        // 推入新状态
        var newState = new DialogState
        {
            CurrentIntent = "control_device",
            SlotValues = new Dictionary<string, object>
            {
                ["device"] = "空调",
                ["location"] = "客厅"
            },
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        await _stateManager.PushStateAsync(newState);
        Console.WriteLine($"栈深度: {_stateManager.StackDepth}"); // 1

        // 获取当前状态
        var current = await _stateManager.GetCurrentStateAsync();
        Console.WriteLine($"当前意图: {current!.CurrentIntent}");

        // 弹出状态
        var popped = await _stateManager.PopStateAsync();
        Console.WriteLine($"弹出状态: {popped!.CurrentIntent}");
    }
}
```

### 话题切换

```csharp
public async Task HandleTopicSwitch()
{
    var context = new DialogContext { SessionId = "session1" };

    // 当前状态: 控制设备
    var currentState = new DialogState
    {
        CurrentIntent = "control_device",
        SlotValues = new Dictionary<string, object>
        {
            ["device"] = "灯",
            ["location"] = "客厅"
        }
    };
    await _stateManager.PushStateAsync(currentState);

    // 用户切换到音乐播放
    var newIntent = "PlayMusic";
    var shouldSave = await _stateManager.HandleTopicSwitchAsync(newIntent);

    if (shouldSave)
    {
        // 保存新状态
        var musicState = new DialogState
        {
            CurrentIntent = "PlayMusic",
            SlotValues = new Dictionary<string, object>
            {
                ["artist"] = "周杰伦"
            }
        };
        await _stateManager.PushStateAsync(musicState);

        // 栈深度: 2 (可以回退)
    }
}
```

### 状态回退

```csharp
public async Task RollbackToPreviousState()
{
    // 用户说 "回到刚才"
    var success = await _stateManager.RollbackAsync();

    if (success)
    {
        var previousState = await _stateManager.GetCurrentStateAsync();
        Console.WriteLine($"回退到: {previousState!.CurrentIntent}");
        // 可以继续之前的话题
    }
}
```

## 完整示例：多轮对话流程

### 场景：设备控制 + 澄清 + 指代消解 + 话题切换

```csharp
public class SmartHomeDialogFlow
{
    private readonly ISlotManager _slotManager;
    private readonly ICoreferenceResolver _coreferenceResolver;
    private readonly IntentDriftDetector _driftDetector;
    private readonly DialogStateManager _stateManager;
    private readonly IMafSessionStorage _sessionStorage;

    public async Task HandleCompleteFlow()
    {
        var conversationId = "conv1";
        var context = new DialogContext { SessionId = conversationId, UserId = "user1" };

        // ========== Turn 1: 用户说 "打开空调" ==========
        await HandleTurn1("打开空调", conversationId, context);

        // ========== Turn 2: 系统澄清 "哪个房间？" ==========
        var clarification = "客厅的";
        await HandleTurn2(clarification, conversationId, context);

        // ========== Turn 3: 用户说 "把它调到26度" ==========
        await HandleTurn3("把它调到26度", conversationId, context);

        // ========== Turn 4: 用户说 "对了，播放周杰伦的歌" ==========
        await HandleTurn4("对了，播放周杰伦的歌", conversationId, context);

        // ========== Turn 5: 用户说 "回到刚才" ==========
        await HandleTurn5("回到刚才", conversationId, context);
    }

    private async Task HandleTurn1(string input, string convId, DialogContext ctx)
    {
        Console.WriteLine($"[Turn 1] 用户: {input}");

        // 检测槽位
        var intent = "control_device";
        var slots = await _slotManager.DetectSlotsAsync(intent, input);

        // 填充已知槽位
        await _slotManager.FillSlotAsync(ctx, intent, "device", "空调");
        await _slotManager.FillSlotAsync(ctx, intent, "action", "打开");

        // 检查缺失
        var missing = _slotManager.GetMissingSlots(intent, slots);
        if (missing.Contains("location"))
        {
            var questions = _slotManager.GenerateClarificationQuestions(intent, missing);
            Console.WriteLine($"[系统] {questions[0].Question}");
            // 输出: "请问您想控制哪个位置的空调？"
        }
    }

    private async Task HandleTurn2(string input, string convId, DialogContext ctx)
    {
        Console.WriteLine($"[Turn 2] 用户: {input}");

        // 填充缺失槽位
        var intent = "control_device";
        await _slotManager.FillSlotAsync(ctx, intent, "location", "客厅");

        // 保存对话历史
        await _sessionStorage.AddMessageAsync(convId, new MafAiMessage
        {
            Role = "user",
            Content = "打开客厅的空调"
        });

        // 保存状态
        var state = new DialogState
        {
            CurrentIntent = intent,
            SlotValues = new Dictionary<string, object>
            {
                ["device"] = "空调",
                ["location"] = "客厅",
                ["action"] = "打开"
            }
        };
        await _stateManager.PushStateAsync(state);

        Console.WriteLine("[系统] 好的，已打开客厅的空调");
    }

    private async Task HandleTurn3(string input, string convId, DialogContext ctx)
    {
        Console.WriteLine($"[Turn 3] 用户: {input}");

        // 指代消解
        var resolved = await _coreferenceResolver.ResolveAsync(input, convId);
        Console.WriteLine($"[消解后] {resolved}"); // "把空调调到26度"

        // 检测意图漂移（应该没有）
        var drift = await _driftDetector.DetectDriftAsync(input, "control_device", ctx);
        drift.HasDrifted.Should().BeFalse();

        // 填充新槽位
        await _slotManager.FillSlotAsync(ctx, "control_device", "adjustment", "26度");

        Console.WriteLine("[系统] 已将客厅空调调到26度");
    }

    private async Task HandleTurn4(string input, string convId, DialogContext ctx)
    {
        Console.WriteLine($"[Turn 4] 用户: {input}");

        // 检测意图漂移
        var drift = await _driftDetector.DetectDriftAsync(input, "control_device", ctx);
        drift.HasDrifted.Should().BeTrue(); // 检测到 "对了"

        // 处理话题切换
        var shouldSave = await _stateManager.HandleTopicSwitchAsync("PlayMusic");
        shouldSave.Should().BeTrue();

        // 推入新状态
        var musicState = new DialogState
        {
            CurrentIntent = "PlayMusic",
            SlotValues = new Dictionary<string, object>
            {
                ["artist"] = "周杰伦"
            }
        };
        await _stateManager.PushStateAsync(musicState);

        Console.WriteLine("[系统] 好的，开始播放周杰伦的歌");
    }

    private async Task HandleTurn5(string input, string convId, DialogContext ctx)
    {
        Console.WriteLine($"[Turn 5] 用户: {input}");

        // 回退到上一个状态
        var success = await _stateManager.RollbackAsync();
        success.Should().BeTrue();

        var previous = await _stateManager.GetCurrentStateAsync();
        Console.WriteLine($"[系统] 已回到: {previous!.CurrentIntent}");
        // 输出: "已回到: control_device"
    }
}
```

## 集成测试示例

### 测试槽位填充流程

```csharp
[Fact]
public async Task SlotFilling_CompleteFlow_ShouldSucceed()
{
    // Arrange
    var slotManager = new SlotManager(provider, registry, logger);
    var context = new DialogContext { SessionId = "test" };

    // Act
    var slots = await slotManager.DetectSlotsAsync("control_device", "打开客厅的灯");
    await slotManager.FillSlotAsync(context, "control_device", "device", "灯");
    await slotManager.FillSlotAsync(context, "control_device", "location", "客厅");

    // Assert
    var filled = await slotManager.GetFilledSlotsAsync(context, "control_device");
    filled["device"].Value.Should().Be("灯");
    filled["location"].Value.Should().Be("客厅");
}
```

### 测试指代消解

```csharp
[Fact]
public async Task CoreferenceResolution_WithPronoun_ShouldResolve()
{
    // Arrange
    var sessionStorage = new TestSessionStorage();
    var resolver = new MafCoreferenceResolver(sessionStorage, logger);
    var convId = "conv1";

    await sessionStorage.AddMessageAsync(convId, new MafAiMessage
    {
        Role = "user",
        Content = "打开客厅的空调"
    });

    // Act
    var resolved = await resolver.ResolveAsync("把它调低一点", convId);

    // Assert
    resolved.Should().Contain("空调");
}
```

### 测试意图漂移

```csharp
[Fact]
public async Task IntentDrift_WithTriggerWord_ShouldDetect()
{
    // Arrange
    var detector = new IntentDriftDetector(logger);
    var context = new DialogContext { SessionId = "session1" };

    // Act
    var analysis = await detector.DetectDriftAsync(
        "顺便问一下，今天天气怎么样",
        "ControlLight",
        context
    );

    // Assert
    analysis.HasDrifted.Should().BeTrue();
    analysis.SuggestedAction.Should().Be(DriftAction.NewTopic);
}
```

## 最佳实践

### 1. 槽位设计

```csharp
// ✅ 好的设计：清晰的槽位定义
var goodDefinition = new SlotDefinition
{
    IntentName = "control_device",
    RequiredSlots = new List<SlotTemplate>
    {
        new() { SlotName = "device", SlotType = SlotType.String },
        new() { SlotName = "location", SlotType = SlotType.String, IsOptional = true }
    }
};

// ❌ 不好的设计：槽位定义不清晰
var badDefinition = new SlotDefinition
{
    IntentName = "control_device",
    RequiredSlots = new List<SlotTemplate>
    {
        new() { SlotName = "param1", SlotType = SlotType.Object }, // 不明确的名称
        new() { SlotName = "data", SlotType = SlotType.Array } // 过于通用
    }
};
```

### 2. 错误处理

```csharp
public async Task SafeSlotFilling()
{
    try
    {
        var slots = await _slotManager.DetectSlotsAsync(intent, input);

        if (slots.Count == 0)
        {
            // 回退到通用处理
            await HandleGenericInput(input);
            return;
        }

        var missing = _slotManager.GetMissingSlots(intent, slots);
        if (missing.Count > 3)
        {
            // 缺失太多，可能是意图识别错误
            await RequestRephrase();
            return;
        }

        // 正常处理...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "槽位填充失败");
        await HandleError();
    }
}
```

### 3. 性能优化

```csharp
// 使用缓存避免重复检测
public class CachedSlotManager
{
    private readonly Dictionary<string, SlotDetectionResult> _cache = new();

    public async Task<SlotDetectionResult> DetectWithCacheAsync(string intent, string input)
    {
        var key = $"{intent}:{input}";

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = await _slotManager.DetectSlotsAsync(intent, input);
        _cache[key] = result;
        return result;
    }
}
```

### 4. 状态管理

```csharp
// 限制栈深度避免内存溢出
public async Task PushStateWithLimitAsync(DialogState state)
{
    if (_stateManager.StackDepth >= 10)
    {
        await _stateManager.ClearAllAsync(); // 或者只弹出最旧的状态
    }

    await _stateManager.PushStateAsync(state);
}
```

## 总结

对话管理组件提供了完整的多轮对话支持：

- **SlotManager**: 槽位检测、填充、验证
- **MafCoreferenceResolver**: 指代消解，理解上下文
- **IntentDriftDetector**: 意图漂移检测，处理话题切换
- **DialogStateManager**: 对话状态管理，支持回退和多话题

通过组合使用这些组件，可以构建复杂的多轮对话系统。
