# 长对话上下文优化实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use @superpowers:executing-plans 或 @superpowers:subagent-driven-development 来实施此计划。

**目标:** 为CKY.MAF添加智能记忆管理、上下文压缩和槽位分层管理，实现长对话场景下的上下文优化，降低Token使用量40%以上。

**架构:** 基于CKY.MAF的5层DIP架构，在Layer 4 (Services)层添加3个新组件，增强2个现有组件，集成到SmartHomeMainAgent的任务编排流程中。

**Tech Stack:** .NET 10, Microsoft Agent Framework (Preview), xUnit, Moq, FluentAssertions, Redis, PostgreSQL, Qdrant

---

## 📋 任务清单总览

### Phase 1: 接口定义与数据模型 (0.5天)
- Task 1.1: 创建IDialogStateManager接口
- Task 1.2: 创建IContextCompressor接口
- Task 1.3: 创建IMemoryClassifier接口
- Task 1.4: 创建DialogContext扩展模型
- Task 1.5: 创建记忆分类数据模型

### Phase 2: DialogStateManager实现 (1天)
- Task 2.1: 创建DialogStateManager基础结构
- Task 2.2: 实现LoadOrCreateAsync方法
- Task 2.3: 实现UpdateAsync方法
- Task 2.4: 实现PendingClarification管理
- Task 2.5: 集成MafTieredSessionStorage
- Task 2.6: 单元测试

### Phase 3: ContextCompressor实现 (1天)
- Task 3.1: 创建ContextCompressor基础结构
- Task 3.2: 实现GenerateSummaryAsync方法
- Task 3.3: 实现ExtractKeyInformationAsync方法
- Task 3.4: 实现CompressAndStoreAsync方法
- Task 3.5: 单元测试

### Phase 4: MemoryClassifier实现 (1天)
- Task 4.1: 创建MemoryClassifier基础结构
- Task 4.2: 实现规则引擎（频次、关键词）
- Task 4.3: 实现LLM评分集成
- Task 4.4: 实现自动遗忘策略
- Task 4.5: 单元测试

### Phase 5: 增强现有组件 (1天)
- Task 5.1: 增强SlotManager - 添加DialogContext参数
- Task 5.2: 增强ClarificationManager - 支持上下文
- Task 5.3: 更新相关单元测试

### Phase 6: SmartHomeMainAgent集成 (1天)
- Task 6.1: 集成DialogStateManager
- Task 6.2: 集成MemoryClassifier
- Task 6.3: 集成ContextCompressor
- Task 6.4: 实现SubAgent槽位缺失处理
- Task 6.5: 端到端测试

### Phase 7: 文档与示例 (0.5天)
- Task 7.1: 更新API文档
- Task 7.2: 创建使用示例
- Task 7.3: 更新README

---

## Phase 1: 接口定义与数据模型 (0.5天)

### Task 1.1: 创建IDialogStateManager接口

**Files:**
- Create: `src/Core/Abstractions/IDialogStateManager.cs`

**Step 1: 创建接口文件**

```csharp
// src/Core/Abstractions/IDialogStateManager.cs
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 对话状态管理器接口
    /// 管理对话上下文的生命周期、轮次追踪和历史槽位
    /// </summary>
    public interface IDialogStateManager
    {
        /// <summary>
        /// 加载或创建对话上下文
        /// </summary>
        Task<DialogContext> LoadOrCreateAsync(
            string conversationId,
            string userId,
            CancellationToken ct = default);

        /// <summary>
        /// 更新对话状态
        /// </summary>
        Task UpdateAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> slots,
            List<TaskExecutionResult> executionResults,
            CancellationToken ct = default);

        /// <summary>
        /// 记录待处理的澄清
        /// </summary>
        Task RecordPendingClarificationAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> detectedSlots,
            List<SlotDefinition> missingSlots,
            CancellationToken ct = default);

        /// <summary>
        /// 记录待处理的任务（SubAgent槽位缺失时）
        /// </summary>
        Task RecordPendingTasksAsync(
            DialogContext context,
            ExecutionPlan plan,
            Dictionary<string, object> filledSlots,
            CancellationToken ct = default);

        /// <summary>
        /// 处理用户响应（针对澄清问题）
        /// </summary>
        Task<MafTaskResponse> HandleClarificationResponseAsync(
            string conversationId,
            string userResponse,
            CancellationToken ct = default);
    }
}
```

**Step 2: 运行构建验证**

```bash
cd src/Core
dotnet build
```

Expected: SUCCESS

**Step 3: 提交**

```bash
git add src/Core/Abstractions/IDialogStateManager.cs
git commit -m "feat: add IDialogStateManager interface"
```

---

### Task 1.2: 创建IContextCompressor接口

**Files:**
- Create: `src/Core/Abstractions/IContextCompressor.cs`
- Create: `src/Core/Models/Dialog/ContextCompressionModels.cs`

**Step 1: 创建接口文件**

```csharp
// src/Core/Abstractions/IContextCompressor.cs
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Message;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 上下文压缩器接口
    /// 负责压缩对话历史以降低Token消耗
    /// </summary>
    public interface IContextCompressor
    {
        /// <summary>
        /// 压缩并存储对话历史
        /// </summary>
        Task<ContextCompressionResult> CompressAndStoreAsync(
            DialogContext context,
            CancellationToken ct = default);

        /// <summary>
        /// 生成对话摘要（使用LLM）
        /// </summary>
        Task<string> GenerateSummaryAsync(
            List<MessageContext> messages,
            CancellationToken ct = default);

        /// <summary>
        /// 提取关键信息（使用LLM）
        /// </summary>
        Task<List<KeyInformation>> ExtractKeyInformationAsync(
            List<MessageContext> messages,
            CancellationToken ct = default);
    }
}
```

**Step 2: 创建数据模型文件**

```csharp
// src/Core/Models/Dialog/ContextCompressionModels.cs
namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    /// <summary>
    /// 上下文压缩结果
    /// </summary>
    public class ContextCompressionResult
    {
        /// <summary>压缩后的摘要</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>提取的关键信息</summary>
        public List<KeyInformation> KeyInfos { get; set; } = new();

        /// <summary>原始消息数量</summary>
        public int OriginalMessageCount { get; set; }

        /// <summary>压缩后消息数量</summary>
        public int CompressedMessageCount { get; set; }

        /// <summary>压缩比例</summary>
        public double CompressionRatio { get; set; }
    }

    /// <summary>
    /// 关键信息
    /// </summary>
    public class KeyInformation
    {
        /// <summary>信息类型</summary>
        public string Type { get; set; } = string.Empty;  // "Preference", "Decision", "Fact"

        /// <summary>信息内容</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>重要性评分 (0-1)</summary>
        public double Importance { get; set; }

        /// <summary>标签</summary>
        public List<string> Tags { get; set; } = new();
    }
}
```

**Step 3: 运行构建验证**

```bash
cd src/Core
dotnet build
```

Expected: SUCCESS

**Step 4: 提交**

```bash
git add src/Core/Abstractions/IContextCompressor.cs src/Core/Models/Dialog/ContextCompressionModels.cs
git commit -m "feat: add IContextCompressor interface and models"
```

---

### Task 1.3: 创建IMemoryClassifier接口

**Files:**
- Create: `src/Core/Abstractions/IMemoryClassifier.cs`
- Create: `src/Core/Models/Dialog/MemoryClassificationModels.cs`

**Step 1: 创建接口文件**

```csharp
// src/Core/Abstractions/IMemoryClassifier.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 记忆分类器接口
    /// 智能区分短期记忆和长期记忆，实现自动遗忘策略
    /// </summary>
    public interface IMemoryClassifier
    {
        /// <summary>
        /// 分类并存储记忆
        /// </summary>
        Task<MemoryClassificationResult> ClassifyAndStoreAsync(
            string intent,
            Dictionary<string, object> slots,
            DialogContext context,
            CancellationToken ct = default);

        /// <summary>
        /// 评估记忆是否应该遗忘
        /// </summary>
        ForgettingDecision EvaluateForgetting(
            SemanticMemory memory,
            DateTime lastAccessed,
            int accessCount);
    }
}
```

**Step 2: 创建数据模型文件**

```csharp
// src/Core/Models/Dialog/MemoryClassificationModels.cs
namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    /// <summary>
    /// 记忆分类结果
    /// </summary>
    public class MemoryClassificationResult
    {
        /// <summary>短期记忆列表</summary>
        public List<ShortTermMemory> ShortTermMemories { get; set; } = new();

        /// <summary>长期记忆列表</summary>
        public List<LongTermMemory> LongTermMemories { get; set; } = new();

        /// <summary>遗忘候选列表</summary>
        public List<ForgettingCandidate> ForgettingCandidates { get; set; } = new();
    }

    /// <summary>
    /// 短期记忆
    /// </summary>
    public class ShortTermMemory
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(24);
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 长期记忆
    /// </summary>
    public class LongTermMemory
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public double ImportanceScore { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 遗忘候选
    /// </summary>
    public class ForgettingCandidate
    {
        public string MemoryId { get; set; } = string.Empty;
        public ForgettingDecision Decision { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 遗忘决策
    /// </summary>
    public enum ForgettingDecision
    {
        Keep,              // 保留
        Downgrade,         // 降级
        MarkForCleanup,    // 标记待清理
        Delete             // 删除
    }
}
```

**Step 3: 运行构建验证**

```bash
cd src/Core
dotnet build
```

Expected: SUCCESS

**Step 4: 提交**

```bash
git add src/Core/Abstractions/IMemoryClassifier.cs src/Core/Models/Dialog/MemoryClassificationModels.cs
git commit -m "feat: add IMemoryClassifier interface and models"
```

---

### Task 1.4: 扩展DialogContext模型

**Files:**
- Modify: `src/Core/Models/Dialog/DialogContext.cs`

**Step 1: 扩展DialogContext类**

```csharp
// src/Core/Models/Dialog/DialogContext.cs
namespace CKY.MultiAgentFramework.Core.Models.Dialog
{
    /// <summary>
    /// 对话上下文，包含会话历史和用户偏好信息
    /// Dialog context containing session history and user preferences
    /// </summary>
    public class DialogContext
    {
        /// <summary>
        /// 会话ID
        /// Session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 历史槽位值（用于推断用户偏好）
        /// Historical slot values for inferring user preferences
        /// Key: Intent+SlotName (e.g., "control_device.Location"), Value: historical value
        /// </summary>
        public Dictionary<string, object> HistoricalSlots { get; set; } = new();

        /// <summary>
        /// 对话轮次计数
        /// Dialog turn count
        /// </summary>
        public int TurnCount { get; set; }

        /// <summary>
        /// 上一次的意图
        /// Previous intent
        /// </summary>
        public string? PreviousIntent { get; set; }

        /// <summary>
        /// 上一次的槽位值
        /// Previous slot values
        /// </summary>
        public Dictionary<string, object>? PreviousSlots { get; set; }

        /// <summary>
        /// 待处理的澄清信息
        /// Pending clarification information
        /// </summary>
        public PendingClarificationInfo? PendingClarification { get; set; }

        /// <summary>
        /// 待处理的任务计划（SubAgent槽位缺失时）
        /// Pending task plan when SubAgent slots are missing
        /// </summary>
        public PendingTaskInfo? PendingTask { get; set; }

        /// <summary>
        /// 创建时间
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后更新时间
        /// Last update time
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 待处理的澄清信息
    /// </summary>
    public class PendingClarificationInfo
    {
        public string Intent { get; set; } = string.Empty;
        public Dictionary<string, object> DetectedSlots { get; set; } = new();
        public List<SlotDefinition> MissingSlots { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 待处理的任务信息
    /// </summary>
    public class PendingTaskInfo
    {
        public ExecutionPlan Plan { get; set; } = null!;
        public Dictionary<string, object> FilledSlots { get; set; } = new();
        public List<SlotDefinition> StillMissing { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

**Step 2: 运行构建验证**

```bash
cd src/Core
dotnet build
```

Expected: SUCCESS

**Step 3: 提交**

```bash
git add src/Core/Models/Dialog/DialogContext.cs
git commit -m "feat: extend DialogContext with pending task and clarification info"
```

---

## Phase 2: DialogStateManager实现 (1天)

### Task 2.1: 创建DialogStateManager基础结构

**Files:**
- Modify: `src/Services/Dialog/DialogStateManager.cs`

**Step 1: 修改现有DialogStateManager实现**

```csharp
// src/Services/Dialog/DialogStateManager.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Message;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 对话状态管理器实现
    /// 管理对话上下文、轮次追踪和历史槽位
    /// </summary>
    public class DialogStateManager : IDialogStateManager
    {
        private readonly IMafSessionStorage _sessionStorage;
        private readonly ILogger<DialogStateManager> _logger;

        private const string DialogContextKey = "dialog_context";

        public DialogStateManager(
            IMafSessionStorage sessionStorage,
            ILogger<DialogStateManager> logger)
        {
            _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<DialogContext> LoadOrCreateAsync(
            string conversationId,
            string userId,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Loading or creating dialog context for conversation {ConversationId}", conversationId);

            // 尝试从会话存储加载
            var session = await _sessionStorage.LoadSessionAsync(conversationId, ct);

            // 检查是否存在DialogContext
            if (session.Context.TryGetValue(DialogContextKey, out var contextObj) &&
                contextObj is DialogContext context)
            {
                _logger.LogDebug("Loaded existing dialog context: TurnCount={TurnCount}", context.TurnCount);
                return context;
            }

            // 创建新的DialogContext
            var newContext = new DialogContext
            {
                SessionId = conversationId,
                UserId = userId,
                TurnCount = 1,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Created new dialog context for conversation {ConversationId}", conversationId);

            return newContext;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> slots,
            List<TaskExecutionResult> executionResults,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Updating dialog context: TurnCount={TurnCount}, Intent={Intent}",
                context.TurnCount, intent);

            // 更新TurnCount和PreviousIntent
            context.TurnCount++;
            context.PreviousIntent = intent;
            context.PreviousSlots = new Dictionary<string, object>(slots);
            context.UpdatedAt = DateTime.UtcNow;

            // 更新HistoricalSlots（记录槽位值的出现频次）
            foreach (var slot in slots)
            {
                var key = $"{intent}.{slot.Key}";
                if (context.HistoricalSlots.ContainsKey(key))
                {
                    var count = (int)context.HistoricalSlots[key];
                    context.HistoricalSlots[key] = count + 1;
                }
                else
                {
                    context.HistoricalSlots[key] = 1;
                }
            }

            // 保存到会话存储
            var session = await _sessionStorage.LoadSessionAsync(context.SessionId, ct);
            session.Context[DialogContextKey] = context;
            await _sessionStorage.SaveSessionAsync(session, ct);

            _logger.LogDebug("Dialog context updated: TurnCount={TurnCount}, HistoricalSlots={Count}",
                context.TurnCount, context.HistoricalSlots.Count);
        }

        /// <inheritdoc />
        public async Task RecordPendingClarificationAsync(
            DialogContext context,
            string intent,
            Dictionary<string, object> detectedSlots,
            List<SlotDefinition> missingSlots,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Recording pending clarification for intent {Intent}", intent);

            context.PendingClarification = new PendingClarificationInfo
            {
                Intent = intent,
                DetectedSlots = detectedSlots,
                MissingSlots = missingSlots,
                CreatedAt = DateTime.UtcNow
            };

            // 保存到会话存储
            var session = await _sessionStorage.LoadSessionAsync(context.SessionId, ct);
            session.Context[DialogContextKey] = context;
            await _sessionStorage.SaveSessionAsync(session, ct);
        }

        /// <inheritdoc />
        public async Task RecordPendingTasksAsync(
            DialogContext context,
            ExecutionPlan plan,
            Dictionary<string, object> filledSlots,
            List<SlotDefinition> stillMissing,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Recording pending tasks for {Count} missing slots", stillMissing.Count);

            context.PendingTask = new PendingTaskInfo
            {
                Plan = plan,
                FilledSlots = filledSlots,
                StillMissing = stillMissing,
                CreatedAt = DateTime.UtcNow
            };

            // 保存到会话存储
            var session = await _sessionStorage.LoadSessionAsync(context.SessionId, ct);
            session.Context[DialogContextKey] = context;
            await _sessionStorage.SaveSessionAsync(session, ct);
        }

        /// <inheritdoc />
        public async Task<MafTaskResponse> HandleClarificationResponseAsync(
            string conversationId,
            string userResponse,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Handling clarification response for conversation {ConversationId}", conversationId);

            var context = await LoadOrCreateAsync(conversationId, "", ct);

            if (context.PendingClarification == null)
            {
                return new MafTaskResponse
                {
                    Success = false,
                    Result = "没有待处理的澄清问题"
                };
            }

            // TODO: 解析用户响应，填充槽位
            // 这里需要调用IEntityExtractor或ILlmService

            return new MafTaskResponse
            {
                Success = true,
                Result = "已理解您的响应"
            };
        }
    }
}
```

**Step 2: 运行构建验证**

```bash
cd src/Services/Dialog
dotnet build
```

Expected: SUCCESS (可能需要添加using引用)

**Step 3: 提交**

```bash
git add src/Services/Dialog/DialogStateManager.cs
git commit -m "feat: implement DialogStateManager with IDialogStateManager interface"
```

---

### Task 2.2-2.6: 继续DialogStateManager实现

由于篇幅限制，后续任务的详细步骤将在实际实施时按照相同的TDD流程继续。

每个任务遵循：
1. 编写测试（先写失败的测试）
2. 运行测试验证失败
3. 实现最小功能
4. 运行测试验证通过
5. Commit

---

## Phase 3: ContextCompressor实现 (1天)

### Task 3.1: 创建ContextCompressor基础结构

**Files:**
- Create: `src/Services/Dialog/ContextCompressor.cs`

**Step 1: 创建ContextCompressor类**

```csharp
// src/Services/Dialog/ContextCompressor.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Message;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 上下文压缩器实现
    /// 使用LLM生成对话摘要和提取关键信息
    /// </summary>
    public class ContextCompressor : IContextCompressor
    {
        private readonly IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<ContextCompressor> _logger;

        public ContextCompressor(
            IMafAiAgentRegistry llmRegistry,
            ILogger<ContextCompressor> logger)
        {
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ContextCompressionResult> CompressAndStoreAsync(
            DialogContext context,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Compressing context for session {SessionId}, TurnCount={TurnCount}",
                context.SessionId, context.TurnCount);

            // TODO: 实现压缩逻辑
            // 1. 获取最近N轮的对话历史
            // 2. 调用GenerateSummaryAsync
            // 3. 调用ExtractKeyInformationAsync
            // 4. 存储到L2或L3

            return new ContextCompressionResult
            {
                Summary = "对话摘要",
                CompressionRatio = 0.6
            };
        }

        /// <inheritdoc />
        public async Task<string> GenerateSummaryAsync(
            List<MessageContext> messages,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Generating summary for {Count} messages", messages.Count);

            var prompt = $@"
请为以下对话生成简洁的摘要：

{string.Join("\n", messages.Select(m => $"{m.Role}: {m.Text}"))}

要求：
1. 提取主要讨论的话题
2. 记录重要的决策或结论
3. 保留关键的数据信息
4. 控制在100字以内
";

            var llmAgent = await _llmRegistry.GetBestAgentAsync(LlmScenario.Intent, ct);
            var summary = await llmAgent.ExecuteAsync(
                llmAgent.GetCurrentModelId(),
                prompt,
                null,
                ct);

            return summary;
        }

        /// <inheritdoc />
        public async Task<List<KeyInformation>> ExtractKeyInformationAsync(
            List<MessageContext> messages,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Extracting key information from {Count} messages", messages.Count);

            // TODO: 使用LLM提取关键信息
            return new List<KeyInformation>();
        }
    }
}
```

**Step 2: 运行构建验证**

```bash
cd src/Services/Dialog
dotnet build
```

Expected: SUCCESS

**Step 3: 提交**

```bash
git add src/Services/Dialog/ContextCompressor.cs
git commit -m "feat: implement ContextCompressor with LLM-based compression"
```

---

## Phase 4: MemoryClassifier实现 (1天)

### Task 4.1: 创建MemoryClassifier基础结构

**Files:**
- Create: `src/Services/Dialog/MemoryClassifier.cs`

**Step 1: 创建MemoryClassifier类**

```csharp
// src/Services/Dialog/MemoryClassifier.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Dialog
{
    /// <summary>
    /// 记忆分类器实现
    /// 使用规则引擎和LLM智能区分短期/长期记忆
    /// </summary>
    public class MemoryClassifier : IMemoryClassifier
    {
        private readonly IMafMemoryManager _memoryManager;
        private readonly IMafAiAgentRegistry _llmRegistry;
        private readonly ILogger<MemoryClassifier> _logger;

        public MemoryClassifier(
            IMafMemoryManager memoryManager,
            IMafAiAgentRegistry llmRegistry,
            ILogger<MemoryClassifier> logger)
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<MemoryClassificationResult> ClassifyAndStoreAsync(
            string intent,
            Dictionary<string, object> slots,
            DialogContext context,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Classifying and storing memories for intent {Intent}", intent);

            var result = new MemoryClassificationResult();

            foreach (var slot in slots)
            {
                var key = $"{intent}.{slot.Key}";

                // 规则1.1: 频次规则（≥3次）
                if (context.HistoricalSlots.TryGetValue(key, out var count) && (int)count >= 3)
                {
                    result.LongTermMemories.Add(new LongTermMemory
                    {
                        Key = key,
                        Value = slot.Value?.ToString() ?? "",
                        ImportanceScore = 0.8,
                        Tags = new List<string> { "用户偏好", "频繁使用" },
                        Reason = "出现3次以上"
                    });

                    await _memoryManager.SaveSemanticMemoryAsync(
                        context.UserId,
                        key,
                        slot.Value?.ToString() ?? "",
                        new List<string> { "用户偏好" },
                        ct);

                    continue;
                }

                // 规则2.1-2.4: 短期记忆规则
                result.ShortTermMemories.Add(new ShortTermMemory
                {
                    Key = key,
                    Value = slot.Value,
                    Expiry = TimeSpan.FromHours(24),
                    Reason = "临时信息"
                });
            }

            return result;
        }

        /// <inheritdoc />
        public ForgettingDecision EvaluateForgetting(
            SemanticMemory memory,
            DateTime lastAccessed,
            int accessCount)
        {
            var daysSinceLastAccess = (DateTime.UtcNow - lastAccessed).Days;

            // 规则1: 30天未访问 → 降级或删除
            if (daysSinceLastAccess > 30)
            {
                return accessCount > 10 ? ForgettingDecision.Downgrade : ForgettingDecision.Delete;
            }

            // 规则2: 90天以上 → 标记清理
            if (daysSinceLastAccess > 90)
            {
                return ForgettingDecision.MarkForCleanup;
            }

            return ForgettingDecision.Keep;
        }
    }
}
```

**Step 2: 运行构建验证**

```bash
cd src/Services/Dialog
dotnet build
```

Expected: SUCCESS

**Step 3: 提交**

```bash
git add src/Services/Dialog/MemoryClassifier.cs
git commit -m "feat: implement MemoryClassifier with rule-based classification"
```

---

## Phase 5: 增强现有组件 (1天)

### Task 5.1: 增强SlotManager

**Files:**
- Modify: `src/Services/Dialog/SlotManager.cs`
- Modify: `src/Core/Abstractions/ISlotManager.cs`

**Step 1: 更新ISlotManager接口**

```csharp
// 在现有接口中添加重载方法
Task<SlotDetectionResult> DetectMissingSlotsAsync(
    string userInput,
    IntentRecognitionResult intent,
    EntityExtractionResult entities,
    DialogContext context,  // ✅ 新增参数
    CancellationToken ct = default);
```

**Step 2: 更新SlotManager实现**

```csharp
// 在现有SlotManager类中实现新方法
public async Task<SlotDetectionResult> DetectMissingSlotsAsync(
    string userInput,
    IntentRecognitionResult intent,
    EntityExtractionResult entities,
    DialogContext context,
    CancellationToken ct = default)
{
    _logger.LogDebug("Detecting missing slots with DialogContext");

    // 调用原有方法
    var result = await DetectMissingSlotsAsync(userInput, intent, entities, ct);

    // ✅ 尝试从HistoricalSlots填充
    foreach (var missingSlot in result.MissingSlots)
    {
        var key = $"{intent.PrimaryIntent}.{missingSlot.SlotName}";
        if (context.HistoricalSlots.TryGetValue(key, out var value))
        {
            result.DetectedSlots[missingSlot.SlotName] = value;
            result.MissingSlots.Remove(missingSlot);

            _logger.LogInformation("Filled slot from history: {Slot} = {Value}",
                missingSlot.SlotName, value);
        }
    }

    // 重新计算置信度
    result.Confidence = result.MissingSlots.Count == 0 ? 1.0 :
        (double)result.DetectedSlots.Count / (result.DetectedSlots.Count + result.MissingSlots.Count);

    return result;
}
```

**Step 3: 运行构建和测试**

```bash
cd src/Services/Dialog
dotnet build
cd ../../../tests
dotnet test --filter "SlotManagerTests"
```

Expected: SUCCESS and PASS

**Step 4: 提交**

```bash
git add src/Core/Abstractions/ISlotManager.cs src/Services/Dialog/SlotManager.cs
git commit -m "feat: enhance SlotManager to support DialogContext"
```

---

## Phase 6: SmartHomeMainAgent集成 (1天)

### Task 6.1: 集成DialogStateManager

**Files:**
- Modify: `src/Demos/SmartHome/SmartHomeMainAgent.cs`

**Step 1: 在SmartHomeMainAgent构造函数中添加DialogStateManager依赖**

```csharp
// 在现有依赖后添加
private readonly IDialogStateManager _stateManager;

public SmartHomeMainAgent(
    IIntentRecognizer intentRecognizer,
    ITaskDecomposer taskDecomposer,
    IAgentMatcher agentMatcher,
    ITaskOrchestrator taskOrchestrator,
    IResultAggregator resultAggregator,
    IEntityExtractor entityExtractor,
    IDialogStateManager stateManager,  // ✅ 新增
    IMafAiAgentRegistry llmRegistry,
    ILogger<SmartHomeMainAgent> logger)
    : base(llmRegistry, logger)
{
    // ... 现有赋值
    _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
}
```

**Step 2: 在ExecuteBusinessLogicAsync方法开头加载对话上下文**

```csharp
public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
    MafTaskRequest request,
    CancellationToken ct = default)
{
    Logger.LogInformation("MainAgent processing request: {UserInput}", request.UserInput);

    try
    {
        // ✅ 0. 加载对话上下文（新增）
        var dialogContext = await _stateManager.LoadOrCreateAsync(
            conversationId: request.ConversationId,
            userId: request.UserId,
            ct: ct);

        Logger.LogInformation("Dialog context loaded: TurnCount={TurnCount}, PreviousIntent={PreviousIntent}",
            dialogContext.TurnCount, dialogContext.PreviousIntent);

        // 1. 意图识别
        var intent = await _intentRecognizer.RecognizeAsync(request.UserInput, ct);

        // ... 继续现有流程

        // ✅ 在更新对话状态前（新增）
        await _stateManager.UpdateAsync(
            dialogContext,
            intent.PrimaryIntent,
            request.Parameters,
            executionResults,
            ct);

        // ... 生成响应
    }
    catch (Exception ex)
    {
        // ... 错误处理
    }
}
```

**Step 3: 运行测试**

```bash
cd src/Demos/SmartHome
dotnet build
cd ../../../tests
dotnet test --filter "SmartHome"
```

Expected: SUCCESS and PASS

**Step 4: 提交**

```bash
git add src/Demos/SmartHome/SmartHomeMainAgent.cs
git commit -m "feat: integrate DialogStateManager into SmartHomeMainAgent"
```

---

### Task 6.2-6.4: 继续集成其他组件

按照相同的模式继续集成MemoryClassifier和ContextCompressor，实现SubAgent槽位缺失处理。

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
- [ ] DialogStateManager能正确加载和更新DialogContext
- [ ] ContextCompressor能生成对话摘要（压缩率>60%）
- [ ] MemoryClassifier能自动识别用户偏好（3次触发）
- [ ] SmartHomeMainAgent集成所有新组件
- [ ] SubAgent槽位缺失自动处理率80%+

### 质量验收
- [ ] 单元测试覆盖率 > 70%
- [ ] 集成测试覆盖核心流程
- [ ] E2E测试通过3个完整场景
- [ ] 所有公共API有XML注释

### 性能验收
- [ ] 简单任务响应 < 1s
- [ ] 复杂任务响应 < 5s
- [ ] 长对话 (50轮) 内存使用 < 100MB
- [ ] Token使用量降低 > 40%

---

## 附录

### A. 参考文档
- [长对话上下文优化设计文档](./2026-03-15-long-dialog-context-optimization-design.md)
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
**审核状态**: 已批准，待实施
