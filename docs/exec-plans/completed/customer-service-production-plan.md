# 客服系统生产化 & 业务层数据库使用方案

> **版本**: 1.3.0  
> **日期**: 2026-03-20  
> **状态**: 实施中（Phase 1-4 ✅ 框架增强 ✅）
>
> ### 实施进度
> | 阶段 | 状态 | 完成日期 | 说明 |
> |------|------|---------|------|
> | Phase 1: 数据库基础设施 | ✅ 完成 | 2026-03-19 | 实体、DbContext、EntityConfigs、种子数据、迁移 |
> | Phase 2: 持久化服务实现 | ✅ 完成 | 2026-03-19 | 6个Persistent*Service + Program.cs配置切换 |
> | Phase 3: UI 集成 | ✅ 完成 | 2026-03-19 | Chat.razor 集成持久化聊天历史 |
> | Phase 4: 外部系统集成接口 | ✅ 接口完成 | 2026-03-19 | 接口+Mock实现已完成，待对接真实API |
> | Phase 5: LLM 集成增强 | ⏳ 待定 | - | 根据项目进度决定 |
>
> ### 补充修复与框架增强（Round 4）
> | 项目 | 状态 | 说明 |
> |------|------|------|
> | 单元测试(22个) | ✅ 完成 | PersistentServicesTests.cs 覆盖5个持久化服务 |
> | DI注册测试修复(3个) | ✅ 完成 | MafServiceRegistrationExtensionsTests 预存失败修复 |
> | 全部单元测试 | ✅ 370通过 | 零失败 |
> | SmartHome Demo 页面补全 | ✅ 完成 | Chat.razor + DeviceControl.razor 新增 |
> | PostgreSQL 生产环境配置 | ✅ 完成 | 重试策略、连接池、超时配置 |
> | Qdrant 向量存储 API 修正 | ✅ 完成 | Filter、类型保留、安全ID、死代码清理 |
> | LLM HttpClient 工厂支持 | ✅ 完成 | AddLlmAgentFactory + 6个命名HttpClient |
> | 集成测试覆盖提升 | ✅ 完成 | 新增7个测试文件、+64个测试用例（99个集成测试通过） |
> | 全部测试 | ✅ 475通过 | 370单元 + 105集成（1个Redis需Docker，预期中） |

---

## 一、现状分析

### 1.1 关系数据库使用现状

| 维度 | MAF 框架层 | 客服业务层 |
|------|-----------|-----------|
| **ORM** | EF Core（MafDbContext）+ Dapper（IRelationalDatabase） | 无 |
| **数据持久化** | ✅ 完整（任务、计划、会话、LLM配置） | ❌ 全是内存模拟 |
| **Repository 模式** | ✅ IMainTaskRepository / ISubTaskRepository / IUnitOfWork | ❌ 不存在 |
| **数据库迁移** | ✅ EF Core Migrations | ❌ 不存在 |
| **独立 DbContext** | N/A（框架用 MafDbContext） | ❌ 不存在 |

**核心问题**：

1. **框架与业务数据库边界不清**：当前 MafDbContext 只管理框架实体，业务应用没有自己的 DbContext，缺乏独立的数据模型和迁移管理。
2. **双轨 ORM 选择困惑**：SmartHome 用 EF Core（`AddMafBuiltinServices`），CustomerService 用 Dapper（`AddMafDapperServices`），缺乏统一的业务层数据访问指导。
3. **Dapper 路线缺少 Repository 抽象**：IRelationalDatabase 是通用 CRUD 接口，业务语义弱，不适合复杂业务逻辑。

### 1.2 客服系统现状

| 功能模块 | 状态 | 说明 |
|---------|------|------|
| 意图识别 | ✅ 完成 | 规则引擎 + 关键词匹配 |
| 实体提取 | ✅ 完成 | 基于意图驱动 |
| 情感检测 | ✅ 完成 | 关键词+自动升级 |
| Agent 路由 | ✅ 完成 | 主Agent→专业Agent |
| 多轮对话 | ✅ 完成 | 内存管理，含压缩 |
| 降级策略 | ✅ 完成 | 5级降级+规则引擎兜底 |
| **订单服务** | ⚠️ 模拟 | SimulatedOrderService，内存字典 |
| **工单服务** | ⚠️ 模拟 | SimulatedTicketService，内存字典 |
| **知识库** | ⚠️ 模拟 | 5条FAQ，关键词匹配 |
| **用户行为** | ⚠️ 模拟 | 内存记录，无分析 |
| **数据持久化** | ❌ 缺失 | 应用重启全部丢失 |
| **对话记录持久化** | ❌ 缺失 | 仅前端内存 |
| **用户认证** | ❌ 缺失 | 硬编码用户ID |
| **LLM 调用** | ❌ 缺失 | 只走规则引擎分支 |

---

## 二、整体设计方案

### 2.1 设计原则

1. **框架与业务分离**：MAF 框架数据库（MafDbContext）与业务数据库（CustomerServiceDbContext）独立管理，互不侵入
2. **遵循 DIP 架构**：业务层通过接口依赖，数据访问实现在基础设施层
3. **渐进式改造**：保留现有模拟服务接口不变，新增持久化实现逐步替换
4. **信息检索接口化**：外部系统（ERP、CRM）通过接口调用，当前写调用代码，后续对接
5. **开发环境零配置**：SQLite 开箱即用，生产切 PostgreSQL

### 2.2 架构总览

```
┌─────────────────────────────────────────────────────────────────────┐
│                     客服系统 (Layer 5 - Demo)                       │
│                                                                     │
│  Program.cs                                                         │
│  ├── AddMafDapperServices()          → MAF 框架数据                 │
│  ├── AddCustomerServiceDbContext()   → 业务数据（新增）              │
│  └── 业务服务注册                                                    │
│                                                                     │
│  Blazor UI (Chat.razor)                                             │
│  └── 对话交互、工单管理、订单查询                                    │
└───────────────┬─────────────────────┬───────────────────────────────┘
                │                     │
    ┌───────────▼──────────┐  ┌──────▼──────────────────────┐
    │ MAF 框架服务          │  │ 客服业务服务                  │
    │ (Layer 4 - Services)  │  │ (CustomerService 内部)       │
    │                       │  │                              │
    │ • 意图识别             │  │ • IOrderService              │
    │ • 任务调度             │  │ • ITicketService             │
    │ • 会话管理             │  │ • IKnowledgeBaseService      │
    │ • Agent 编排           │  │ • IChatHistoryService（新增） │
    │ • 降级管理             │  │ • ICustomerService（新增）    │
    └───────────┬──────────┘  └──────┬──────────────────────┘
                │                     │
    ┌───────────▼──────────┐  ┌──────▼──────────────────────┐
    │ MAF 基础设施          │  │ 客服数据访问（新增）          │
    │ (Layer 3)             │  │                              │
    │                       │  │ • CustomerServiceDbContext    │
    │ • MafDbContext         │  │ • OrderRepository            │
    │ • DapperRelational     │  │ • TicketRepository           │
    │ • Redis/Memory         │  │ • ChatHistoryRepository      │
    │ • Qdrant/Memory        │  │ • CustomerRepository         │
    └───────────┬──────────┘  │ • FaqRepository               │
                │              └──────┬──────────────────────┘
                │                     │
    ┌───────────▼──────────┐  ┌──────▼──────────────────────┐
    │ maf_framework.db      │  │ customer_service.db          │
    │ (框架数据库)           │  │ (业务数据库)                 │
    │                       │  │                              │
    │ • MainTasks            │  │ • Customers                  │
    │ • SubTasks             │  │ • Orders + OrderItems        │
    │ • SchedulePlans        │  │ • Tickets + TicketComments   │
    │ • Sessions             │  │ • ChatSessions + Messages    │
    │ • LlmProviderConfigs   │  │ • FaqEntries                 │
    └──────────────────────┘  │ • UserBehaviorRecords         │
                               └──────────────────────────────┘
```

### 2.3 关键设计决策

| 决策点 | 方案 | 理由 |
|--------|------|------|
| 业务层 ORM | EF Core（独立 DbContext） | 类型安全，迁移管理，与框架一致 |
| 框架 vs 业务数据库 | 开发共享、生产分离 | 开发简单，生产可独立扩展 |
| 现有接口 | 保留不变 | IOrderService 等接口不改，只换实现 |
| 外部系统集成 | 接口+调用代码 | 当前写适配器骨架，后续对接真实API |
| 对话记录 | 新增 IChatHistoryService | 独立管理，不依赖 MAF Session |
| 用户体系 | 新增简单客户模型 | 非认证系统，仅客户信息管理 |

---

## 三、详细实施计划

### 阶段 1：业务层数据库基础设施（P0）

**目标**：建立客服系统独立的数据访问层，解决"业务层如何使用关系数据库"的核心问题。

#### 1.1 设计业务实体模型

在客服 Demo 项目中创建 EF Core 实体：

```
src/Demos/CustomerService/
├── Data/
│   ├── CustomerServiceDbContext.cs          # 业务 DbContext
│   ├── CustomerServiceDbContextFactory.cs   # 设计时工厂（迁移用）
│   ├── EntityConfigurations/
│   │   ├── CustomerEntityConfiguration.cs
│   │   ├── OrderEntityConfiguration.cs
│   │   ├── TicketEntityConfiguration.cs
│   │   ├── ChatSessionEntityConfiguration.cs
│   │   ├── ChatMessageEntityConfiguration.cs
│   │   ├── FaqEntryEntityConfiguration.cs
│   │   └── UserBehaviorEntityConfiguration.cs
│   └── Migrations/                          # EF Core 自动生成
├── Entities/                                # 数据库实体（与业务模型分离）
│   ├── CustomerEntity.cs
│   ├── OrderEntity.cs
│   ├── OrderItemEntity.cs
│   ├── TicketEntity.cs
│   ├── TicketCommentEntity.cs
│   ├── ChatSessionEntity.cs
│   ├── ChatMessageEntity.cs
│   ├── FaqEntryEntity.cs
│   └── UserBehaviorRecordEntity.cs
```

**核心实体设计**：

```csharp
// 客户信息
public class CustomerEntity
{
    public int Id { get; set; }
    public string CustomerId { get; set; }    // 业务ID（如 CUST-001）
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PreferredLanguage { get; set; } = "zh-CN";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }

    // 导航属性
    public List<OrderEntity> Orders { get; set; }
    public List<TicketEntity> Tickets { get; set; }
    public List<ChatSessionEntity> ChatSessions { get; set; }
}

// 订单
public class OrderEntity
{
    public int Id { get; set; }
    public string OrderId { get; set; }       // 业务ID（如 ORD-2024-001）
    public int CustomerId { get; set; }       // FK
    public string Status { get; set; }        // pending/paid/shipped/delivered/cancelled
    public decimal TotalAmount { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // 导航
    public CustomerEntity Customer { get; set; }
    public List<OrderItemEntity> Items { get; set; }
}

// 工单
public class TicketEntity
{
    public int Id { get; set; }
    public string TicketId { get; set; }      // 业务ID（如 TKT-20260319-001）
    public int CustomerId { get; set; }       // FK
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }      // order/product/payment/shipping/other
    public string Priority { get; set; }      // low/normal/high/urgent
    public string Status { get; set; }        // open/in_progress/resolved/closed
    public string? RelatedOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // 导航
    public CustomerEntity Customer { get; set; }
    public List<TicketCommentEntity> Comments { get; set; }
}

// 对话记录
public class ChatSessionEntity
{
    public int Id { get; set; }
    public string SessionId { get; set; }     // 会话ID
    public int? CustomerId { get; set; }      // FK（可为匿名）
    public string Status { get; set; }        // active/closed
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Summary { get; set; }      // 对话摘要

    // 导航
    public CustomerEntity? Customer { get; set; }
    public List<ChatMessageEntity> Messages { get; set; }
}

// 对话消息
public class ChatMessageEntity
{
    public int Id { get; set; }
    public int ChatSessionId { get; set; }    // FK
    public string Role { get; set; }          // user/assistant/system
    public string Content { get; set; }
    public string? Intent { get; set; }       // 识别的意图
    public string? EntitiesJson { get; set; } // 提取的实体 JSON
    public DateTime Timestamp { get; set; }

    // 导航
    public ChatSessionEntity ChatSession { get; set; }
}

// FAQ 知识库条目
public class FaqEntryEntity
{
    public int Id { get; set; }
    public string Question { get; set; }
    public string Answer { get; set; }
    public string Category { get; set; }
    public string KeywordsJson { get; set; }  // 关键词 JSON 数组
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### 1.2 创建 CustomerServiceDbContext

```csharp
public class CustomerServiceDbContext : DbContext
{
    public CustomerServiceDbContext(DbContextOptions<CustomerServiceDbContext> options)
        : base(options) { }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();
    public DbSet<TicketEntity> Tickets => Set<TicketEntity>();
    public DbSet<TicketCommentEntity> TicketComments => Set<TicketCommentEntity>();
    public DbSet<ChatSessionEntity> ChatSessions => Set<ChatSessionEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();
    public DbSet<FaqEntryEntity> FaqEntries => Set<FaqEntryEntity>();
    public DbSet<UserBehaviorRecordEntity> UserBehaviorRecords => Set<UserBehaviorRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerServiceDbContext).Assembly);
    }
}
```

#### 1.3 服务注册

```csharp
// Program.cs 中新增
builder.Services.AddMafDapperServices(builder.Configuration);  // MAF 框架（保持不变）

// 客服业务数据库（新增）
builder.Services.AddDbContext<CustomerServiceDbContext>(options =>
{
    var provider = builder.Configuration["CustomerService:Database:Provider"] ?? "SQLite";
    if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite("Data Source=customer_service.db");
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("CustomerService"));
    }
});
```

---

### 阶段 2：持久化服务实现（P0）

**目标**：将现有模拟服务替换为数据库持久化实现，保持接口不变。

#### 2.1 实现持久化服务

```
src/Demos/CustomerService/
├── Services/
│   ├── ICustomerServiceInterfaces.cs         # 保持不变
│   ├── IChatHistoryService.cs                # 新增：对话记录服务接口
│   ├── ICustomerService.cs                   # 新增：客户信息服务接口
│   ├── Implementations/
│   │   ├── SimulatedOrderService.cs          # 保留（作为降级备选）
│   │   ├── SimulatedTicketService.cs         # 保留
│   │   ├── SimulatedKnowledgeBaseService.cs  # 保留
│   │   ├── SimulatedUserBehaviorService.cs   # 保留
│   │   ├── PersistentOrderService.cs         # 新增：EF Core 实现
│   │   ├── PersistentTicketService.cs        # 新增
│   │   ├── PersistentKnowledgeBaseService.cs # 新增
│   │   ├── PersistentChatHistoryService.cs   # 新增
│   │   ├── PersistentCustomerService.cs      # 新增
│   │   └── PersistentUserBehaviorService.cs  # 新增
```

**实现策略**：

| 服务 | 当前实现 | 持久化实现 | 关键改造点 |
|------|---------|-----------|-----------|
| IOrderService | SimulatedOrderService | PersistentOrderService | 内存字典 → EF Core CRUD |
| ITicketService | SimulatedTicketService | PersistentTicketService | 字典 → DB + 自动编号 |
| IKnowledgeBaseService | SimulatedKnowledgeBaseService | PersistentKnowledgeBaseService | 5条FAQ → DB存储 + 种子数据 |
| IChatHistoryService | 不存在 | PersistentChatHistoryService | 全新：对话记录持久化 |
| ICustomerService | 不存在 | PersistentCustomerService | 全新：客户信息管理 |
| IUserBehaviorService | SimulatedUserBehaviorService | PersistentUserBehaviorService | 内存 → DB 记录 |

#### 2.2 新增接口定义

```csharp
/// <summary>
/// 对话历史服务接口
/// </summary>
public interface IChatHistoryService
{
    /// <summary>创建新的对话会话</summary>
    Task<string> CreateSessionAsync(string? customerId, CancellationToken ct = default);

    /// <summary>保存对话消息</summary>
    Task SaveMessageAsync(string sessionId, string role, string content,
        string? intent = null, Dictionary<string, string>? entities = null,
        CancellationToken ct = default);

    /// <summary>获取会话的对话历史</summary>
    Task<List<ChatMessageEntity>> GetSessionMessagesAsync(string sessionId,
        int? limit = null, CancellationToken ct = default);

    /// <summary>关闭会话</summary>
    Task CloseSessionAsync(string sessionId, string? summary = null,
        CancellationToken ct = default);

    /// <summary>获取客户的历史会话列表</summary>
    Task<List<ChatSessionEntity>> GetCustomerSessionsAsync(string customerId,
        int pageSize = 20, CancellationToken ct = default);
}

/// <summary>
/// 客户信息服务接口
/// </summary>
public interface ICustomerService
{
    /// <summary>根据客户ID查询</summary>
    Task<CustomerEntity?> GetCustomerAsync(string customerId, CancellationToken ct = default);

    /// <summary>创建或更新客户</summary>
    Task<CustomerEntity> UpsertCustomerAsync(string customerId, string name,
        string? email = null, string? phone = null, CancellationToken ct = default);

    /// <summary>更新最后活跃时间</summary>
    Task UpdateLastActiveAsync(string customerId, CancellationToken ct = default);
}
```

#### 2.3 种子数据

为开发环境提供初始化数据，让系统开箱可用：

```
src/Demos/CustomerService/
├── Data/
│   ├── SeedData/
│   │   └── CustomerServiceSeedData.cs    # 种子数据初始化
```

包含：
- **3 个客户**：VIP会员、普通用户、新注册用户
- **8 个订单**：覆盖各种状态（待付款、已发货、已完成、已取消等）
- **5 个工单**：覆盖各种优先级和类别
- **20+ 条 FAQ**：覆盖常见客服问题
- **模拟物流数据**：关联到已发货订单

---

### 阶段 3：对话记录与 UI 集成（P1）

**目标**：Chat.razor 集成对话记录持久化，实现对话历史查看。

#### 3.1 Chat.razor 改造

```
当前流程：
用户输入 → MainAgent处理 → 结果显示在前端内存列表

改造后流程：
用户输入 → IChatHistoryService.SaveMessage(user)
         → MainAgent处理
         → IChatHistoryService.SaveMessage(assistant)
         → 结果显示
         → 可查看历史会话
```

#### 3.2 新增 UI 功能

| 功能 | 组件 | 说明 |
|------|------|------|
| 对话记录保存 | Chat.razor 改造 | 每条消息自动保存到数据库 |
| 历史会话列表 | ChatHistory.razor（新增） | 查看过往对话 |
| 工单管理面板 | TicketPanel.razor（新增） | 查看/管理工单 |
| 客户信息卡片 | CustomerCard.razor（新增） | 显示当前客户信息 |

---

### 阶段 4：外部系统集成接口（P1）

**目标**：定义外部系统调用接口，写好调用代码骨架。

#### 4.1 外部系统适配器

```
src/Demos/CustomerService/
├── ExternalApis/
│   ├── IExternalOrderApi.cs              # 外部订单系统接口
│   ├── IExternalLogisticsApi.cs          # 外部物流系统接口
│   ├── IExternalPaymentApi.cs            # 外部支付系统接口
│   ├── MockExternalOrderApi.cs           # 模拟实现（当前使用）
│   ├── MockExternalLogisticsApi.cs       # 模拟实现
│   └── MockExternalPaymentApi.cs         # 模拟实现
```

**设计模式**：适配器模式

```csharp
/// <summary>
/// 外部订单系统 API（适配器接口）
/// 后续对接真实 ERP 系统时，替换此实现
/// </summary>
public interface IExternalOrderApi
{
    /// <summary>查询订单（调用外部 ERP）</summary>
    Task<ExternalOrderResponse?> QueryOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>取消订单（调用外部 ERP）</summary>
    Task<ExternalCancelResponse> CancelOrderAsync(string orderId, string reason, CancellationToken ct = default);

    /// <summary>申请退款（调用外部支付系统）</summary>
    Task<ExternalRefundResponse> RequestRefundAsync(string orderId, decimal amount, string reason, CancellationToken ct = default);
}

/// <summary>
/// 外部物流 API
/// </summary>
public interface IExternalLogisticsApi
{
    /// <summary>查询物流轨迹</summary>
    Task<ExternalTrackingResponse?> GetTrackingAsync(string trackingNumber, CancellationToken ct = default);
}

// PersistentOrderService 中的使用方式：
public class PersistentOrderService : IOrderService
{
    private readonly CustomerServiceDbContext _dbContext;
    private readonly IExternalOrderApi _externalApi;

    public async Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct)
    {
        // 优先查本地数据库（缓存/已同步数据）
        var localOrder = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (localOrder != null)
            return MapToOrderInfo(localOrder);

        // 本地没有，调用外部系统
        var externalOrder = await _externalApi.QueryOrderAsync(orderId, ct);
        if (externalOrder == null)
            return null;

        // 同步到本地数据库
        var entity = MapToEntity(externalOrder);
        _dbContext.Orders.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return MapToOrderInfo(entity);
    }
}
```

---

### 阶段 5：LLM 集成增强（P2）

**目标**：接入真实 LLM，提升智能客服能力。

> 此阶段根据项目进度决定是否实施，当前以规则引擎+降级兜底为主。

#### 5.1 改造点

| 改造项 | 说明 |
|--------|------|
| MainAgent 接入 LLM | 使用 MafAiAgent + LlmAgentFactory |
| 意图识别升级 | HybridIntentRecognizer（规则+LLM） |
| 知识库 RAG | QdrantVectorStore + LLM 生成答案 |
| 对话摘要 | LLM 自动生成对话摘要 |

---

## 附录 A：项目全局待处理问题清单

> 除客服系统外，项目还存在以下待处理问题，需统一规划。

### A.1 框架层待处理问题（与客服系统有交集）

| # | 问题 | 优先级 | 当前状态 | 与客服系统的关系 |
|---|------|--------|---------|-----------------|
| 1 | PostgreSQL 生产环境配置优化 | P0 | ⏳ 未完成 | 客服系统生产部署依赖此项 |
| 2 | LLM Agent HttpClient 工厂 | P0 | ⏳ 未完成 | 客服阶段5 LLM集成依赖此项 |
| 3 | Qdrant 向量存储 API 调整 | P0 | 95% | 客服 RAG 知识库依赖此项 |
| 4 | SmartHome Demo Pages | P1 | 30% | 无直接关系 |
| 5 | 集成测试覆盖度不足（9.8% vs 目标 > 60%） | P1 | 10% | 客服持久化需要集成测试 |
| 6 | Prometheus 监控指标不完善 | P2 | 60% | 客服系统监控依赖此项 |
| 7 | 文档不一致与维护滞后 | P2 | 已修正 | 需同步更新 |

### A.2 建议的统一优先级排序

**第一批（当前轮次 - 客服系统核心）**：
1. 客服系统阶段 1-2（数据库基础设施 + 持久化服务） ← **本次实施**
2. 客服系统阶段 3（UI 集成对话持久化）

**第二批（下一轮次 - 框架补齐）**：
3. PostgreSQL 生产配置优化
4. LLM HttpClient Factory
5. Qdrant API 调整
6. 客服系统阶段 4（外部系统集成）

**第三批（后续轮次 - 质量提升）**：
7. 集成测试补充
8. SmartHome Demo Pages
9. Prometheus 指标完善
10. 客服系统阶段 5（LLM 集成）

---

## 四、文件变更清单

### 新增文件

```
src/Demos/CustomerService/
├── Data/
│   ├── CustomerServiceDbContext.cs
│   ├── CustomerServiceDbContextFactory.cs
│   ├── SeedData/
│   │   └── CustomerServiceSeedData.cs
│   ├── EntityConfigurations/
│   │   ├── CustomerEntityConfiguration.cs
│   │   ├── OrderEntityConfiguration.cs
│   │   ├── OrderItemEntityConfiguration.cs
│   │   ├── TicketEntityConfiguration.cs
│   │   ├── TicketCommentEntityConfiguration.cs
│   │   ├── ChatSessionEntityConfiguration.cs
│   │   ├── ChatMessageEntityConfiguration.cs
│   │   ├── FaqEntryEntityConfiguration.cs
│   │   └── UserBehaviorRecordEntityConfiguration.cs
│   └── Migrations/                           # EF Core 自动生成
├── Entities/
│   ├── CustomerEntity.cs
│   ├── OrderEntity.cs
│   ├── OrderItemEntity.cs
│   ├── TicketEntity.cs
│   ├── TicketCommentEntity.cs
│   ├── ChatSessionEntity.cs
│   ├── ChatMessageEntity.cs
│   ├── FaqEntryEntity.cs
│   └── UserBehaviorRecordEntity.cs
├── Services/
│   ├── IChatHistoryService.cs
│   ├── ICustomerService.cs
│   ├── Implementations/
│   │   ├── PersistentOrderService.cs
│   │   ├── PersistentTicketService.cs
│   │   ├── PersistentKnowledgeBaseService.cs
│   │   ├── PersistentChatHistoryService.cs
│   │   ├── PersistentCustomerService.cs
│   │   └── PersistentUserBehaviorService.cs
├── ExternalApis/
│   ├── IExternalOrderApi.cs
│   ├── IExternalLogisticsApi.cs
│   ├── IExternalPaymentApi.cs
│   ├── MockExternalOrderApi.cs
│   ├── MockExternalLogisticsApi.cs
│   └── MockExternalPaymentApi.cs
├── Components/Pages/
│   ├── ChatHistory.razor                     # 历史会话页面
│   ├── TicketPanel.razor                     # 工单管理（可选）
│   └── CustomerCard.razor                    # 客户信息（可选）
```

### 修改文件

```
src/Demos/CustomerService/
├── Program.cs                                # 新增 DbContext 注册、切换服务实现
├── CKY.MAF.Demos.CustomerService.csproj      # 新增 EF Core 包引用
├── appsettings.json                          # 新增业务数据库配置
├── Components/Pages/Chat.razor               # 集成对话持久化
```

---

## 五、实施优先级与依赖关系

```
阶段 1：数据库基础设施          阶段 2：持久化服务
┌─────────────────────┐      ┌─────────────────────┐
│ 1.1 实体模型设计     │──┐   │ 2.1 PersistentOrder  │
│ 1.2 DbContext        │  │   │ 2.2 PersistentTicket │
│ 1.3 EntityConfig     │  ├──▶│ 2.3 PersistentKB     │
│ 1.4 迁移             │  │   │ 2.4 PersistentChat   │
│ 1.5 种子数据         │──┘   │ 2.5 PersistentUser   │
│ 1.6 Program.cs 注册  │      │ 2.6 外部API接口       │
└─────────────────────┘      └──────────┬──────────┘
                                         │
                              ┌──────────▼──────────┐
                              │ 阶段 3：UI 集成      │
                              │ 3.1 Chat.razor 改造  │
                              │ 3.2 历史会话页面      │
                              │ 3.3 Program.cs 切换   │
                              └──────────┬──────────┘
                                         │
                              ┌──────────▼──────────┐
                              │ 阶段 4：外部系统      │
                              │ 4.1 API 适配器       │
                              │ 4.2 数据同步机制      │
                              └──────────┬──────────┘
                                         │
                              ┌──────────▼──────────┐
                              │ 阶段 5：LLM 集成     │
                              │  （后续规划）         │
                              └─────────────────────┘
```

---

## 六、关键技术决策记录

### Q1：为什么不直接扩展 MafDbContext？

**答**：违反 DIP 原则。MafDbContext 属于 MAF 框架（Layer 3），客服系统是 Demo 应用（Layer 5）。业务表不应该侵入框架的数据上下文。独立 DbContext 支持：
- 独立迁移管理
- 独立部署和扩展
- 框架升级不影响业务表

### Q2：为什么选 EF Core 而不是继续用 Dapper？

**答**：
- 客服系统有复杂的实体关系（Customer → Orders → Items，Ticket → Comments）
- EF Core 的导航属性和 Include 比 Dapper 手写 JOIN 更高效
- EF Core Migrations 提供版本化的数据库迁移
- 与 MAF 框架的 Repository 模式保持一致
- Dapper 仍可用于性能关键的查询场景

### Q3：开发环境如何同时使用两个数据库？

**答**：
- MAF 框架：`maf.db`（SQLite，由 `AddMafDapperServices` 配置）
- 客服业务：`customer_service.db`（SQLite，由 `AddDbContext<CustomerServiceDbContext>` 配置）
- 两个独立的 SQLite 文件，互不干扰
- 生产环境可以是同一个 PostgreSQL 服务器的不同数据库

### Q4：现有的模拟服务怎么处理？

**答**：保留模拟服务，作为降级兜底。通过配置切换：

```json
{
  "CustomerService": {
    "UsePersistentStorage": true   // false 则使用模拟服务
  }
}
```

```csharp
if (config.GetValue<bool>("CustomerService:UsePersistentStorage"))
{
    services.AddScoped<IOrderService, PersistentOrderService>();
    services.AddScoped<ITicketService, PersistentTicketService>();
}
else
{
    services.AddSingleton<IOrderService, SimulatedOrderService>();
    services.AddSingleton<ITicketService, SimulatedTicketService>();
}
```

---

## 七、验收标准

### 阶段 1 完成标准
- [ ] CustomerServiceDbContext 创建并可正常迁移
- [ ] 所有实体配置通过 EF Core 验证
- [ ] 种子数据成功初始化
- [ ] 项目编译通过

### 阶段 2 完成标准
- [ ] 所有持久化服务实现完整 CRUD
- [ ] 通过配置可切换模拟/持久化实现
- [ ] 外部 API 接口定义完整
- [ ] 单元测试覆盖核心逻辑

### 阶段 3 完成标准
- [ ] 对话消息实时保存到数据库
- [ ] 可查看历史对话会话列表
- [ ] 应用重启后对话记录不丢失

### 阶段 4 完成标准
- [ ] 外部系统适配器接口定义完整
- [ ] Mock 实现可正常工作
- [ ] 数据同步策略（本地缓存 + 外部调用）验证通过
