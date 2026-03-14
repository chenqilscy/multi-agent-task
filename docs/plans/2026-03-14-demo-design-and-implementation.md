# Demo 设计与实现方案

**日期**: 2026-03-14  
**作者**: Claude Code  
**状态**: 设计完成，实现进行中

---

## 📋 目录

- [1. 系统后端处理逻辑（通用）](#1-系统后端处理逻辑通用)
- [2. Demo1：智能家居](#2-demo1智能家居)
- [3. Demo2：智能客服问答](#3-demo2智能客服问答)
- [4. A2A 编排模式](#4-a2a-编排模式)

---

## 1. 系统后端处理逻辑（通用）

### 1.1 消息接收后的完整处理流程

```
用户消息输入
    ↓
[Step 1] 会话管理
  - 获取/创建会话 (ConversationId)
  - 加载历史上下文 (Session State)
  - 注入用户画像 (User Profile)
    ↓
[Step 2] 预处理
  - 分词、标准化
  - 指代消解 (MafCoreferenceResolver) —— "那个" 指向前文实体
  - 多轮语境融合 —— 合并历史实体到当前查询
    ↓
[Step 3] 意图识别 (IIntentRecognizer)
  - HybridIntentRecognizer：规则优先 + LLM 兜底
  - 输出：PrimaryIntent + Confidence + AlternativeIntents
  - 低置信度时进入澄清流程
    ↓
[Step 4] 实体提取 (IEntityExtractor)
  - IntentDrivenEntityExtractor：根据意图选择对应 Provider
  - 两阶段：关键字匹配 → LLM 增强（触发条件：输入长 / 覆盖率低 / 包含模糊词）
  - 缺失必需实体 → 触发澄清流程
    ↓
[Step 5] 澄清判断
  - 检查必需实体是否齐全
  - 不齐全 → 生成澄清问题，暂停执行，等待用户补充
  - 齐全 → 继续执行
    ↓
[Step 6] 任务分解 (ITaskDecomposer)
  - 将复合命令拆解为多个子任务
  - 例："打开客厅空调，关闭灯，开卧室灯" → 3 个子任务
  - 分析依赖关系，确定串行/并行执行策略
    ↓
[Step 7] Agent 匹配 (IAgentMatcher)
  - 根据子任务意图和能力标签匹配最合适的 Agent
  - 支持负载均衡和失败回退
    ↓
[Step 8] 任务编排与执行 (ITaskOrchestrator)
  - 独立子任务并行执行
  - 有依赖的子任务串行执行
  - 实时状态追踪，SignalR 推送进度
  - 单个任务失败不影响其他独立任务
    ↓
[Step 9] 结果聚合 (IResultAggregator)
  - 收集所有子任务结果
  - 合并成统一的用户友好响应
  - 生成自然语言摘要
    ↓
[Step 10] 会话状态更新
  - 持久化执行结果
  - 更新用户画像（行为偏好）
  - 更新实体记忆（供下次引用）
    ↓
返回响应给用户
```

### 1.2 编排任务与执行模式

#### A2A 模式（Main-Agent 管理型）

```
用户请求
    ↓
SmartHomeMainAgent（主控 Agent）
  ├── 意图识别、实体提取、任务分解
  ├── 通过能力标签匹配子 Agent
  ├── 创建执行计划（ExecutionPlan）
  ├── 并行调度独立子任务
  │       ├── LightingAgent.ExecuteAsync("关闭灯")
  │       ├── ClimateAgent.ExecuteAsync("打开空调")
  │       └── LightingAgent.ExecuteAsync("开卧室灯")
  ├── 等待所有子任务完成
  └── 聚合结果 → 返回用户
```

---

## 2. Demo1：智能家居

### 2.1 案例1：天气查询

**用户输入**: "今天天气如何？"

#### 实现流程

```
1. 意图识别 → QueryWeather (confidence: 0.92)

2. 实体提取
   必需实体：
   - City（城市）：未提取到 → 触发澄清：「您要查询哪个城市的天气？」
   - 用户补充：「北京」
   可选实体：
   - Date（日期）："今天" → today
   - QueryAspect（查询维度）：天气（temperature, weather_condition）

3. 工具调用
   WeatherAgent.ExecuteAsync() →
     IWeatherService.GetWeatherAsync(city: "北京", date: today)
     返回：{ Temperature: 12, Condition: "多云", AQI: 65, Wind: "北风3级" }

4. 结果格式化
   「北京今天天气：多云，气温12°C，北风3级，空气质量良好。
     穿衣建议：建议穿薄外套或卫衣。
     出行建议：天气适宜出行，无需携带雨具。」
```

#### 关键设计点

- **城市实体缺失澄清**：询问城市；若用户位置已知（GPS/历史），自动填充
- **多维度整合**：除基础天气外，补充穿衣/出行建议，提升体验
- **外部工具接入**：通过 `IWeatherService` 抽象接入第三方天气 API（或模拟）

---

### 2.2 案例2：历史温度查询

**用户输入**: "最近一段时间客厅的温度变化情况？"

#### 实现流程

```
1. 意图识别 → QueryTemperatureHistory (confidence: 0.88)

2. 实体提取
   已提取：
   - Room（房间）："客厅"
   - TimeRange（时间范围）："最近一段时间" → 模糊 → 默认7天
   - SensorType（传感器类型）："温度"

3. 传感器数据查询
   TemperatureHistoryAgent.ExecuteAsync() →
     ISensorDataService.GetTemperatureHistoryAsync(room: "客厅", days: 7)
     返回：[
       { Date: "3/7", Min: 18, Max: 25, Avg: 21.5 },
       { Date: "3/8", Min: 17, Max: 24, Avg: 20.8 },
       ...共7条
     ]

4. 趋势分析 + 结果格式化
   「客厅过去7天温度变化情况：
     • 温度范围：16°C ~ 26°C
     • 平均温度：21.3°C
     • 趋势：整体呈下降趋势（近3天有所降低）
     • 最高温：3月10日 26°C
     • 最低温：3月13日 16°C
     
   建议：近期气温波动较大，建议开启空调恒温功能。」
```

#### 关键设计点

- **模糊时间范围处理**："最近一段时间" → 默认7天，也可询问具体时段
- **趋势分析**：不只返回原始数据，分析趋势（上升/下降/稳定），提供建议
- **可视化数据**：返回结构化数据供前端绘制图表

---

### 2.3 案例3：多设备控制

**用户输入**: "打开客厅的空调，关闭灯，打开卧室的灯"

#### 实现流程

```
1. 意图识别 → MultiDeviceControl (包含 ControlAC + ControlLight × 2)

2. 实体提取 → 识别出3个独立命令：
   SubTask[0]: 打开客厅空调 → { Room: 客厅, Device: 空调, Action: 打开 }
   SubTask[1]: 关闭灯      → { Room: 客厅（语境推断）, Device: 灯, Action: 关闭 }
   SubTask[2]: 打开卧室的灯 → { Room: 卧室, Device: 灯, Action: 打开 }

3. 任务分解
   3个子任务，相互独立，全部并行执行

4. 并行执行
   ┌─ ClimateAgent("打开客厅空调")  → 成功：客厅空调已开启
   ├─ LightingAgent("关闭客厅灯")   → 成功：客厅灯已关闭
   └─ LightingAgent("打开卧室灯")   → 成功：卧室灯已打开
   （三个任务同时执行，总耗时取最长子任务时间）

5. 结果聚合
   「已完成3项操作：
     ✓ 客厅空调已打开（制冷模式，26°C）
     ✓ 客厅灯已关闭
     ✓ 卧室灯已打开（亮度100%）」
```

#### 关键设计点

- **隐式房间推断**："关闭灯" 未指定房间 → 从上下文推断为"客厅"（与前句同房间）
- **并行执行**：独立任务并行，提升响应速度
- **原子性语义**：部分失败时明确告知，其余成功结果不回滚（智能家居场景）

---

### 2.4 用户澄清流程设计

#### 何时触发澄清

1. **缺失必需实体**：意图已识别，但关键实体缺失（如天气查询无城市）
2. **意图置信度低** (< 0.5)：无法确定用户意图时，列出候选意图供用户选择
3. **实体歧义**：同一实体有多个候选值

#### 澄清实现方案

```csharp
// MafTaskResponse 中的澄清字段
public bool NeedsClarification { get; set; }
public string? ClarificationQuestion { get; set; }
public List<string> ClarificationOptions { get; set; } = new();

// SmartHomeMainAgent 中的澄清逻辑
private static ClarificationInfo? CheckEntitiesCompleteness(
    string intent, Dictionary<string, string> entities)
{
    var missing = GetRequiredEntities(intent)
        .Where(e => !entities.ContainsKey(e))
        .ToList();

    if (missing.Count == 0) return null;

    return new ClarificationInfo
    {
        Question = GenerateClarificationQuestion(intent, missing),
        MissingEntities = missing
    };
}
```

#### 多轮澄清状态管理

```
轮1: 用户："今天天气如何？"
     系统："请问您要查询哪个城市的天气？"

轮2: 用户："北京"
     系统：[内部] 从会话上下文合并实体 { City: 北京 }，重新执行
     系统："北京今天天气：多云，12°C..."
```

---

## 3. Demo2：智能客服问答

### 3.1 系统架构

```
用户输入
    ↓
CustomerServiceMainAgent（主控）
  ├── 意图识别
  │    ├── FAQ查询 → KnowledgeBaseAgent
  │    ├── 订单查询/操作 → OrderAgent
  │    ├── 工单提交/跟进 → TicketAgent
  │    └── 情绪安抚 → SentimentAgent（可选）
  ├── 多轮对话管理（长会话）
  ├── 用户行为追踪 → UserBehaviorService
  └── 结果聚合 → 自然语言响应
```

### 3.2 知识库构建（RAG）

#### 知识库类型

| 类型 | 存储方式 | 检索方式 | 适用场景 |
|------|---------|---------|---------|
| 结构化FAQ | 关系型数据库 | 精确匹配 + 关键字搜索 | 标准问答 |
| 文档知识库 | Qdrant（向量DB） | 语义相似度检索 | 复杂知识查询 |
| 产品手册 | PDF解析 → 向量化 | 向量检索 + 重排序 | 产品咨询 |

#### RAG 检索流程

```
用户问题
    ↓
向量化（IVectorStore.SearchAsync）
    ↓
召回 Top-K 相关文档片段
    ↓
重排序（可选）
    ↓
注入 LLM Prompt（作为上下文）
    ↓
LLM 生成答案（基于检索到的知识）
    ↓
答案 + 来源引用
```

#### KnowledgeBaseAgent 实现

```csharp
public class KnowledgeBaseAgent : MafBusinessAgentBase
{
    private readonly IVectorStore _vectorStore;
    private readonly IRelationalDatabase _db;

    public override IReadOnlyList<string> Capabilities =>
        ["faq", "knowledge-query", "product-info"];

    public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request, CancellationToken ct)
    {
        // 1. 向量检索
        var embedding = await GetEmbeddingAsync(request.UserInput, ct);
        var docs = await _vectorStore.SearchAsync(embedding, topK: 5, ct);

        // 2. 知识库精确匹配（补充）
        var exactMatches = await _db.QueryAsync<FaqEntry>(
            "SELECT * FROM Faqs WHERE Keywords LIKE @q",
            new { q = $"%{request.UserInput}%" }, ct);

        // 3. LLM 生成答案
        var context = BuildContext(docs, exactMatches);
        var answer = await CallLlmAsync(
            $"基于以下知识库回答用户问题：\n{context}\n\n用户问题：{request.UserInput}",
            ct);

        return new MafTaskResponse { Success = true, Result = answer };
    }
}
```

### 3.3 对接三方系统策略

#### 订单系统集成

```csharp
public interface IOrderService
{
    Task<OrderInfo> GetOrderAsync(string orderId, CancellationToken ct);
    Task<bool> CancelOrderAsync(string orderId, string reason, CancellationToken ct);
    Task<TrackingInfo> GetShippingStatusAsync(string orderId, CancellationToken ct);
    Task<RefundResult> RequestRefundAsync(string orderId, RefundRequest request, CancellationToken ct);
}
```

#### 工单系统集成

```csharp
public interface ITicketService
{
    Task<string> CreateTicketAsync(TicketCreateRequest request, CancellationToken ct);
    Task<TicketInfo> GetTicketAsync(string ticketId, CancellationToken ct);
    Task<bool> UpdateTicketAsync(string ticketId, TicketUpdateRequest update, CancellationToken ct);
    Task<List<TicketInfo>> GetUserTicketsAsync(string userId, CancellationToken ct);
}
```

#### 集成策略

- **适配器模式**：对每个三方系统创建适配器实现，统一接口
- **重试 + 熔断**：所有三方调用使用框架内置的 `RetryPolicy` 和 `CircuitBreaker`
- **超时控制**：三方 API 设置合理超时（默认5s）
- **降级方案**：三方系统不可用时返回"系统繁忙，请稍后再试"或转人工

### 3.4 长会话（多轮对话）问题与解决方案

| 问题 | 描述 | 解决方案 |
|------|------|---------|
| **上下文丢失** | 多轮后遗忘早期信息 | 滑动窗口 + 关键信息摘要压缩 |
| **指代消解失效** | "那个订单" 无法指向历史实体 | `MafCoreferenceResolver` + 实体记忆 |
| **意图漂移** | 用户话题跳转导致意图混乱 | 意图状态机 + 话题切换检测 |
| **Token 溢出** | 长对话超出 LLM context window | 分段摘要：保留最近N轮 + 历史摘要 |
| **循环确认** | 同一问题反复澄清 | 记录澄清历史，避免重复提问 |
| **会话超时** | 用户长时间不回复后恢复 | 会话状态持久化 + 恢复机制 |
| **并发冲突** | 同一用户多个入口同时发消息 | 会话锁 + 消息队列 |

#### 上下文压缩算法

```csharp
// 会话超过 N 轮时，压缩历史为摘要
public async Task<ConversationContext> CompressContextAsync(
    List<ConversationTurn> history, int maxTurns = 10)
{
    if (history.Count <= maxTurns)
        return new ConversationContext { FullHistory = history };

    // 保留最近 5 轮
    var recent = history.TakeLast(5).ToList();

    // 用 LLM 摘要早期对话
    var toCompress = history.Take(history.Count - 5).ToList();
    var summary = await SummarizeAsync(toCompress);

    return new ConversationContext
    {
        Summary = summary,
        RecentHistory = recent,
        ImportantEntities = ExtractImportantEntities(history)
    };
}
```

### 3.5 用户行为与偏好评估

#### 收集的行为数据

```csharp
public class UserBehaviorRecord
{
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string Intent { get; set; }
    public bool TaskSucceeded { get; set; }
    public int ClarificationRoundsNeeded { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public bool UserSatisfied { get; set; }         // 基于后续行为推断
    public Dictionary<string, string> Entities { get; set; }  // 关注的实体
    public DateTime Timestamp { get; set; }
}
```

#### 用户画像构建

```csharp
public class UserProfile
{
    public string UserId { get; set; }
    public Dictionary<string, int> IntentFrequency { get; set; }   // 常用功能
    public string? PreferredCity { get; set; }                      // 偏好城市
    public Dictionary<string, string> DefaultEntities { get; set; } // 默认实体值
    public List<string> FrequentOrderCategories { get; set; }       // 常购品类
    public double AverageSessionLength { get; set; }
    public DateTime LastActiveTime { get; set; }
}
```

#### 个性化应用

- **自动补全实体**：用户通常查北京天气 → 默认填充城市为北京
- **个性化推荐**：根据历史订单推荐相关产品
- **主动服务**：订单快到货时主动推送，不等用户查询
- **偏好记忆**：记住用户的输入风格（简洁 vs 详细）

### 3.6 基于本系统的实现方案

```csharp
// Program.cs 注册
services.AddSingleton<IVectorStore, QdrantVectorStore>();  // 知识库向量存储
services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();
services.AddSingleton<IOrderService, OrderServiceAdapter>();     // 对接三方订单系统
services.AddSingleton<ITicketService, TicketServiceAdapter>();   // 对接三方工单系统
services.AddSingleton<IUserBehaviorService, UserBehaviorService>();

// Agent 注册
services.AddSingleton<CustomerServiceMainAgent>();
services.AddSingleton<KnowledgeBaseAgent>();
services.AddSingleton<OrderAgent>();
services.AddSingleton<TicketAgent>();
```

---

## 4. A2A 编排模式

### 4.1 Main-Agent 管理模式

```
UserRequest
    ↓
MainAgent（主控）
  ├── 分析：意图识别 + 实体提取 + 任务分解
  ├── 分发：AgentMatcher 匹配最合适的 SubAgent
  ├── 监控：ExecutionPlan 追踪每个 SubAgent 状态
  ├── 聚合：ResultAggregator 合并所有子结果
  └── 响应：生成最终自然语言答复

SubAgents（被动执行）：
  - 只负责自己领域的具体执行
  - 通过 ExecuteBusinessLogicAsync() 接受任务
  - 返回 MafTaskResponse 给 MainAgent
```

### 4.2 业务级 Agent（Main-Agent）设计要点

```csharp
public abstract class BusinessMainAgentBase : MafBusinessAgentBase
{
    // 子 Agent 注册表（通过能力标签查找）
    protected abstract IReadOnlyDictionary<string, MafBusinessAgentBase> SubAgentRegistry { get; }

    // 核心编排流程
    protected async Task<MafTaskResponse> OrchestrateAsync(
        MafTaskRequest request, CancellationToken ct)
    {
        // 1. 意图识别 → 判断意图类型
        var intent = await RecognizeIntentAsync(request.UserInput, ct);

        // 2. 实体提取 → 检查完整性
        var entities = await ExtractEntitiesAsync(request.UserInput, intent, ct);
        var clarification = CheckEntitiesCompleteness(intent.PrimaryIntent, entities);
        if (clarification != null)
        {
            return new MafTaskResponse
            {
                NeedsClarification = true,
                ClarificationQuestion = clarification.Question
            };
        }

        // 3. 任务分解 → 生成子任务列表
        var subtasks = await DecomposeAsync(request.UserInput, intent, entities, ct);

        // 4. 并行/串行调度
        var results = await ExecuteSubtasksAsync(subtasks, ct);

        // 5. 结果聚合
        return await AggregateResultsAsync(results, request.UserInput, ct);
    }
}
```

### 4.3 执行过程信息反馈（实时进度）

使用 SignalR 推送执行进度：

```csharp
// 在 MafHub 中推送进度
await _hubContext.Clients.User(userId).SendAsync(
    "TaskProgress",
    new TaskProgressEvent
    {
        TaskId = taskId,
        SubTaskId = subTaskId,
        AgentName = agent.Name,
        Status = "Running",  // Running / Completed / Failed
        Message = "正在查询天气数据...",
        Progress = 50  // 0-100
    });
```

### 4.4 执行结果整理合并策略

```csharp
// ResultAggregator 合并策略
public class SmartHomeResultAggregator : IResultAggregator
{
    public async Task<string> GenerateResponseAsync(
        AggregatedResult result, CancellationToken ct)
    {
        if (result.IndividualResults.Count == 1)
        {
            // 单任务：直接返回结果
            return result.IndividualResults[0].Message;
        }

        // 多任务：生成汇总
        var lines = new List<string>();
        var successCount = result.IndividualResults.Count(r => r.Success);
        var totalCount = result.IndividualResults.Count;

        lines.Add($"已完成 {successCount}/{totalCount} 项操作：");
        foreach (var r in result.IndividualResults)
        {
            var icon = r.Success ? "✓" : "✗";
            lines.Add($"  {icon} {r.Message}");
        }

        return string.Join("\n", lines);
    }
}
```
