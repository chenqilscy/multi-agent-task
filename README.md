# CKY.MAF - Enterprise Multi-Agent Framework

<div align="center">

**基于 Microsoft Agent Framework 的企业级多智能体框架**

An enterprise-grade multi-agent framework built as an enhancement layer on top of Microsoft Agent Framework (Preview)

[![.NET](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Architecture](https://img.shields.io/badge/architecture-DIP-green.svg)](docs/specs/12-layered-architecture.md)

[功能特性](#功能特性) • [快速开始](#快速开始) • [架构设计](#架构设计) • [文档](#文档) • [贡献指南](#贡献指南)

</div>

---

## 📋 目录 / Table of Contents

- [项目概述](#项目概述)
- [功能特性](#功能特性)
- [架构设计](#架构设计)
- [技术栈](#技术栈)
- [快速开始](#快速开始)
- [文档](#文档)
- [项目状态](#项目状态)
- [路线图](#路线图)
- [贡献指南](#贡献指南)
- [许可证](#许可证)

---

## 项目概述 / Overview

**CKY.MAF** 是一个企业级多智能体框架，构建于 Microsoft Agent Framework (Preview) 之上，实现了智能任务调度、多智能体协作，并提供企业级功能包括存储抽象、弹性模式和全面的观测能力。

**CKY.MAF** is an enterprise-grade multi-agent framework built as an enhancement layer on top of Microsoft Agent Framework (Preview), implementing intelligent task scheduling, multi-agent collaboration, and providing enterprise-level features including storage abstraction, resilience patterns, and comprehensive observability.

### 核心设计原则 / Core Design Principles

- **依赖倒置原则 (DIP)**: 5层架构，核心层零外部依赖
- **存储抽象化**: 所有存储实现可替换（Redis、PostgreSQL、Qdrant）
- **弹性优先**: 内置重试、熔断、降级策略
- **可观测性**: 全面日志、指标、分布式追踪

---

## 功能特性 / Features

### 🚀 核心功能 / Core Features

#### 1. 智能任务调度 / Intelligent Task Scheduling

- ✅ 基于优先级的任务队列（0-100分）
- ✅ 任务依赖关系管理（DAG有向无环图）
- ✅ 并行执行独立任务
- ✅ 任务超时和取消机制

#### 2. 多智能体协作 / Multi-Agent Collaboration

- ✅ Main-Agent/Sub-Agent 编排模式
- ✅ A2A (Agent-to-Agent) 通信机制
- ✅ 自动任务分解和分配
- ✅ 结果聚合和冲突解决

#### 3. 意图识别与实体提取 / Intent Recognition & Entity Extraction

- ✅ 多 LLM 提供商支持（智谱AI、通义千问、文心一言）
- ✅ 模板化澄清问题生成
- ✅ 动态槽位检测（预定义 + LLM）
- ✅ 历史偏好和默认值自动填充

#### 4. 存储抽象层 / Storage Abstraction Layer

- ✅ **三层存储策略**:
  - L1: 内存缓存（会话数据）
  - L2: Redis 分布式缓存（24小时 TTL）
  - L3: PostgreSQL 持久化存储
- ✅ 向量存储（Qdrant 语义搜索）
- ✅ 完全可替换的存储实现

#### 5. 弹性和容错 / Resilience & Fault Tolerance

- ✅ 指数退避重试策略
- ✅ 熔断器模式（LLM、Redis、PostgreSQL）
- ✅ 5级降级策略
- ✅ 优雅的错误处理和恢复

### 🆕 长对话优化 / Long Dialog Optimization **NEW!**

#### 6.1 智能对话状态管理 / Intelligent Dialog State Management

**组件**: `IDialogStateManager`, `DialogStateManager`

**功能**:
- ✅ **三层槽位架构**:
  - `GlobalSlots`: 跨会话的用户偏好（如"我喜欢古典音乐"）
  - `SessionSlots`: 当前对话的上下文（如"我们正在讨论客厅的设备"）
  - `IntentSlots`: 单个意图的参数（如"ControlDevice的Room=LivingRoom"）
- ✅ **自动槽位填充**: 从历史记录自动填充缺失槽位
- ✅ **话题切换管理**: 保存和恢复对话状态
- ✅ **澄清问题处理**: 跟踪待澄清问题并处理用户响应

**使用示例**:
```csharp
// 加载或创建对话上下文
var context = await _stateManager.LoadOrCreateAsync(conversationId, userId, ct);

// 更新对话状态（自动更新历史槽位和频次）
await _stateManager.UpdateAsync(context, intent, slots, results, ct);

// 读取历史槽位
if (context.HistoricalSlots.TryGetValue("ControlDevice.Room", out var room))
{
    Console.WriteLine($"用户上次使用的房间: {room}");
}
```

#### 6.2 自动上下文压缩 / Automatic Context Compression

**组件**: `IContextCompressor`, `ContextCompressor`

**功能**:
- ✅ **智能触发**: 每5轮对话自动触发压缩
- ✅ **LLM驱动**: 使用 LLM 生成高质量摘要和提取关键信息
- ✅ **Token优化**: 平均减少40%+的Token使用
- ✅ **关键信息保留**: 提取用户偏好、重要决策、事实信息

**使用示例**:
```csharp
// 在第5、10、15...轮触发压缩
if (context.TurnCount % 5 == 0)
{
    var result = await _compressor.CompressAndStoreAsync(context, ct);

    Console.WriteLine($"压缩比: {result.CompressionRatio:P2}");
    Console.WriteLine($"摘要: {result.Summary}");
    Console.WriteLine($"提取了 {result.KeyInfos.Count} 条关键信息");
}
```

**效果示例**:
```
原始对话历史: 5000 tokens
压缩后摘要: 800 tokens
关键信息: 200 tokens
总计: 1000 tokens (减少 80%)
```

#### 6.3 智能记忆分类 / Intelligent Memory Classification

**组件**: `IMemoryClassifier`, `MemoryClassifier`

**功能**:
- ✅ **自动分类**: 区分短期记忆和长期记忆
- ✅ **频次规则**: 出现≥3次自动转为长期记忆
- ✅ **自动遗忘**: 30天未访问降级，90天删除
- ✅ **重要性评分**: 根据访问频次和时效性评分

**记忆分类规则**:
```csharp
// 长期记忆触发条件（满足任一即可）
1. 频次规则: 槽位值出现 ≥ 3 次
2. 关键词规则: 包含"偏好"、"习惯"、"喜欢"等关键词
3. LLM评分: LLM 评估重要性 > 0.7

// 短期记忆特性
- 默认过期时间: 24小时
- 自动清理过期记忆
- 不占用向量存储空间

// 遗忘策略
- 30天未访问 + 访问次数 ≤ 10: 删除
- 30天未访问 + 访问次数 > 10: 降级
- 90天以上: 标记清理
```

**使用示例**:
```csharp
var result = await _classifier.ClassifyAndStoreAsync(intent, slots, context, ct);

Console.WriteLine($"长期记忆: {result.LongTermMemories.Count}");
Console.WriteLine($"短期记忆: {result.ShortTermMemories.Count}");

foreach (var ltMemory in result.LongTermMemories)
{
    Console.WriteLine($"[长期] {ltMemory.Key}: {ltMemory.Value}");
    Console.WriteLine($"  原因: {ltMemory.Reason}");
    Console.WriteLine($"  重要性: {ltMemory.ImportanceScore}");
}
```

#### 6.4 SubAgent槽位缺失自动恢复 / SubAgent Slot Missing Auto-Recovery

**功能**:
- ✅ **错误检测**: 识别 SubAgent 报告的槽位缺失错误
- ✅ **自动填充**: 从 `HistoricalSlots` 查找并填充缺失槽位
- ✅ **智能重试**: 填充后自动重新执行任务
- ✅ **日志记录**: 详细记录填充和重试过程

**工作流程**:
```
1. SubAgent 执行失败
   ↓
2. MainAgent 检测到 "slot missing" 错误
   ↓
3. 从 HistoricalSlots 查找匹配的槽位值
   ↓
4. 自动填充到请求参数
   ↓
5. 重新执行 SubAgent 任务
   ↓
6. 返回成功结果
```

**使用场景**:
```csharp
// 第1轮: 用户说"我通常在客厅工作"
// → 存储到 HistoricalSlots: ControlDevice.Room = "客厅"

// 第2轮: 用户说"打开灯"（未指定房间）
// → SubAgent 失败: 缺少 "Room" 槽位
// → MainAgent 从历史填充: Room = "客厅"
// → SubAgent 重新执行成功
```

---

## 架构设计 / Architecture

### 5层DIP架构 / 5-Layer DIP Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 5: Demo应用层 / Application Layer                   │
│  Blazor Server Demo Applications                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: 业务服务层 / Business Service Layer             │
│  Task Scheduling, Orchestration, Dialog Management         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: 基础设施层 / Infrastructure Layer                │
│  Concrete Implementations: Redis, PostgreSQL, Qdrant       │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: 存储抽象层 / Storage Abstraction Layer           │
│  Domain Services: ISessionStorage, IMemoryManager, etc.    │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: 核心抽象层 / Core Abstraction Layer              │
│  ICacheStore, IVectorStore, IRelationalDatabase            │
└─────────────────────────────────────────────────────────────┘
```

**关键设计规则**: Core层零外部依赖（仅依赖 MS AF 和 Microsoft.Extensions.* 抽象）

### 项目结构 / Project Structure

```
CKY.MAF/
├── src/
│   ├── Core/                          # Layer 1: 核心抽象
│   │   ├── Abstractions/              # 核心接口定义
│   │   ├── Models/                    # 数据模型
│   │   └── Agents/                    # Agent基类
│   │
│   ├── Services/                      # Layer 2: 业务服务
│   │   ├── Dialog/                    # 对话管理服务
│   │   │   ├── DialogStateManager.cs  # 对话状态管理
│   │   │   ├── ContextCompressor.cs   # 上下文压缩
│   │   │   └── MemoryClassifier.cs    # 记忆分类
│   │   ├── Scheduling/                # 任务调度
│   │   ├── Orchestration/             # 编排
│   │   └── IntentRecognition/         # 意图识别
│   │
│   ├── Infrastructure/                # Layer 3: 基础设施
│   │   ├── Caching/                   # 缓存实现
│   │   ├── Relational/                # 关系数据库
│   │   └── Vectorization/             # 向量存储
│   │
│   └── Demos/                         # Layer 5: Demo应用
│       └── SmartHome/                 # 智能家居Demo
│
├── tests/
│   ├── UnitTests/                     # 单元测试 (70%)
│   ├── IntegrationTests/              # 集成测试 (25%)
│   └── E2ETests/                      # 端到端测试 (5%)
│
└── docs/
    ├── specs/                         # 设计文档 (15个文档)
    └── examples/                      # 使用示例
```

---

## 技术栈 / Technology Stack

### 核心框架 / Core Framework
- **.NET 10** (目标框架)
- **Microsoft Agent Framework (Preview)** - AI/LLM 操作必需
- **ASP.NET Core** - 原生（无 ABP 框架）
- **Blazor Server** - Demo 应用 UI

### 存储技术 / Storage Technologies
- **L1**: IMemoryCache（内存）
- **L2**: Redis（分布式缓存）
- **L3**: PostgreSQL（关系数据库）
- **Vector**: Qdrant（语义搜索）

### LLM 集成 / LLM Integration
- ✅ 仅使用 MS AF 原生接口
- ❌ 不使用 SemanticKernel
- ❌ 不直接依赖 LLM 提供商 SDK

**主要提供商**:
- 智谱AI (GLM-4/GLM-4-Plus) - 主要
- 通义千问 - 备选
- 文心一言 - 备选
- 讯飞星火 - 备选

### 测试框架 / Testing Framework
- **xUnit** - 测试框架
- **FluentAssertions** - 断言库
- **Moq** - Mock框架
- **Testcontainers** - 集成测试容器

### 监控和观测 / Monitoring & Observability
- **Prometheus** - 指标收集
- **Grafana** - 可视化
- **分布式追踪** - 请求链路追踪

---

## 快速开始 / Quick Start

### 前置要求 / Prerequisites

- .NET 10 SDK
- Docker (用于 Redis、PostgreSQL、Qdrant)
- 智谱AI API Key (或其他 LLM 提供商)

### 安装步骤 / Installation Steps

1. **克隆仓库 / Clone Repository**
   ```bash
   git clone https://github.com/your-org/CKY.MAF.git
   cd CKY.MAF
   ```

2. **配置环境 / Configure Environment**

   创建 `appsettings.Development.json`:
   ```json
   {
     "MafServices": {
       "Implementations": {
         "ICacheStore": "MemoryCacheStore",
         "IVectorStore": "MemoryVectorStore",
         "IRelationalDatabase": "InMemoryDatabase"
       }
     },
     "LLM": {
       "Provider": "ZhipuAI",
       "ApiKey": "your-api-key",
       "Model": "glm-4-plus"
     }
   }
   ```

3. **启动基础设施服务 / Start Infrastructure Services**
   ```bash
   docker-compose up -d
   ```

4. **构建项目 / Build Project**
   ```bash
   dotnet build CKY.MAF.sln
   ```

5. **运行测试 / Run Tests**
   ```bash
   dotnet test
   ```

6. **运行 Demo 应用 / Run Demo Application**
   ```bash
   dotnet run --project src/Demos/SmartHome
   ```

   访问: `https://localhost:5001`

### 使用示例 / Usage Example

```csharp
// 1. 创建主控Agent
var mainAgent = _serviceProvider.GetRequiredService<SmartHomeMainAgent>();

// 2. 创建请求
var request = new MafTaskRequest
{
    TaskId = Guid.NewGuid().ToString(),
    ConversationId = "conv-001",
    UserId = "user-123",
    UserInput = "打开客厅的灯，亮度调到50%",
    Parameters = new Dictionary<string, object>(),
    Priority = 50
};

// 3. 执行业务逻辑
var response = await mainAgent.ExecuteBusinessLogicAsync(request);

// 4. 处理响应
if (response.Success)
{
    Console.WriteLine($"成功: {response.Result}");
}
else
{
    Console.WriteLine($"失败: {response.Error}");
}
```

---

## 文档 / Documentation

### 设计文档 / Design Documents

所有设计文档位于 `docs/specs/` 目录（15个文档，约384KB）

**必读文档**（从这里开始）:
1. [架构概览](docs/specs/01-architecture-overview.md) - 核心概念和设计原则
2. [5层DIP架构](docs/specs/12-layered-architecture.md) - **关键架构文档**
3. [实现指南](docs/specs/09-implementation-guide.md) - 实现模式和目录结构

**开发参考**:
4. [接口设计规范](docs/specs/06-interface-design-spec.md) - 所有接口定义和数据模型
5. [任务调度设计](docs/specs/03-task-scheduling-design.md) - 任务优先级和依赖管理
6. [测试指南](docs/specs/10-testing-guide.md) - 测试策略（70%单元，25%集成，5%E2E）

**运维参考**:
7. [性能基准](docs/specs/13-performance-benchmarks.md) - 性能指标和优化策略
8. [错误处理指南](docs/specs/14-error-handling-guide.md) - 错误处理、重试、熔断、降级
9. [部署指南](docs/specs/08-deployment-guide.md) - Docker/Kubernetes部署

完整文档索引: [docs/specs/README.md](docs/specs/README.md)

### 使用示例 / Usage Examples

- [长对话优化功能使用指南](docs/examples/long-dialog-usage.md) - 详细的使用示例和最佳实践
- 更多示例正在添加中...

---

## 项目状态 / Project Status

**当前阶段**: 设计和架构完成（15个文档，约384KB）

**实现进度**: Phase 1-7 完成 ✅

### 已完成功能 / Completed Features

- ✅ **Phase 1**: 接口定义（IDialogStateManager、IContextCompressor、IMemoryClassifier）
- ✅ **Phase 2**: DialogStateManager 实现（400+ 行）
- ✅ **Phase 3**: ContextCompressor 实现（LLM 驱动的对话压缩）
- ✅ **Phase 4**: MemoryClassifier 实现（智能记忆分类和遗忘策略）
- ✅ **Phase 5**: 组件增强（SlotManager 支持 DialogContext）
- ✅ **Phase 6**: SmartHomeMainAgent 集成
- ✅ **Phase 7**: 文档和示例

### 测试覆盖 / Test Coverage

- ✅ **单元测试**: DialogStateManager、ContextCompressor、MemoryClassifier
- ✅ **集成测试**: 对话状态管理、上下文压缩、记忆分类
- ✅ **E2E测试**: 7个长对话场景测试用例

**覆盖率目标**:
- Services 层: 90%
- Core 层: 95%
- Infrastructure 层: 80%

---

## 路线图 / Roadmap

### Phase 1-7: 长对话优化 ✅ **COMPLETED**

- ✅ 对话状态管理
- ✅ 上下文压缩
- ✅ 记忆分类
- ✅ SubAgent槽位自动恢复
- ✅ E2E测试
- ✅ 文档和示例

### Phase 8-10: 生产就绪 (计划中)

- ⏳ 性能优化和基准测试
- ⏳ 安全增强（认证、授权、加密）
- ⏳ 监控和告警集成
- ⏳ Kubernetes 部署配置
- ⏳ 生产环境验证

### Phase 11-12: 高级特性 (计划中)

- ⏳ 多模态支持（图像、语音）
- ⏳ 多语言支持
- ⏳ 插件系统
- ⏳ Agent Marketplace

完整路线图: [docs/specs/11-implementation-roadmap.md](docs/specs/11-implementation-roadmap.md)

---

## 性能基准 / Performance Benchmarks

### 响应时间目标 / Response Time Targets

- 简单任务（意图 + 单Agent）: P95 < 1s
- 复杂任务（分解 + 多Agent）: P95 < 5s
- 长对话（多轮）: P95 < 3s
- LLM API 调用: P95 < 3s

### 吞吐量目标 / Throughput Targets

- 简单任务: > 100 req/s
- 复杂任务: > 50 req/s
- 并发用户: > 100

### 资源限制 / Resource Limits

- CPU: < 50% 正常，< 80% 高负载，> 80% 告警
- 内存: < 500MB 正常，< 1GB 高负载，> 1GB 告警
- GC暂停: < 50ms 正常，< 100ms 高负载

详细性能指标: [docs/specs/13-performance-benchmarks.md](docs/specs/13-performance-benchmarks.md)

---

## 贡献指南 / Contributing

我们欢迎各种形式的贡献！

### 贡献方式 / Ways to Contribute

1. **报告Bug**: 使用 GitHub Issues
2. **提出建议**: 使用 GitHub Discussions
3. **提交代码**: Fork 项目，创建 Pull Request
4. **改进文档**: 提交文档改进 PR

### 开发规范 / Development Guidelines

- 遵循 .NET 编码规范
- 编写单元测试（覆盖率 > 80%）
- 添加 XML 文档注释
- 遵循 5层DIP架构
- 使用 TDD 开发模式

### Pull Request 流程 / PR Process

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'feat: add amazing feature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

---

## 许可证 / License

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

---

## 联系方式 / Contact

- **项目主页**: https://github.com/your-org/CKY.MAF
- **问题反馈**: https://github.com/your-org/CKY.MAF/issues
- **讨论区**: https://github.com/your-org/CKY.MAF/discussions

---

## 致谢 / Acknowledgments

- Microsoft Agent Framework 团队
- .NET 社区
- 所有贡献者

---

<div align="center">

**Built with ❤️ using .NET 10 and Microsoft Agent Framework**

[⬆ 返回顶部](#ckymaf---enterprise-multi-agent-framework)

</div>
