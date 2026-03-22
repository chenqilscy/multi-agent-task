# CKY.MultiAgentFramework 快速参考卡片

> **最后更新**: 2026-03-16 | **版本**: v2.0

---

## 🎯 一句话介绍

**CKY.MultiAgentFramework = 基于Microsoft Agent Framework的企业级多Agent增强框架**

```
应用层 (Demo) → CKY.MultiAgentFramework增强层 → MS Agent Framework → 基础设施层
```

---

## 🏗️ 5层架构 (30秒理解)

```
┌─────────────────────────────────────────┐
│ Layer 5: Demo应用 (Blazor)              │ UI演示
├─────────────────────────────────────────┤
│ Layer 4: Services (业务逻辑)            │ 调度/编排/意图
├─────────────────────────────────────────┤
│ Layer 3: Repository (数据访问)          │ Redis/EF Core/Qdrant
├─────────────────────────────────────────┤
│ Layer 2: Core (抽象接口)                │ ICacheStore/IVectorStore
├─────────────────────────────────────────┤
│ Layer 1: MS Agent Framework            │ AIAgent/A2A/LLM
└─────────────────────────────────────────┘
```

**依赖规则**: 上层依赖下层抽象接口，Core层零外部依赖

---

## 🔑 核心概念 (60秒掌握)

### 1. 基于MS AF
```csharp
// ✅ 正确：继承MS AF的AIAgent
public class MafAiAgent : AIAgent { }

// ❌ 错误：自己定义IMafAgent接口
```

### 2. 业务Agent不继承AIAgent
```csharp
// ✅ 正确：纯业务基类，组合调用LLM
public class MafBusinessAgentBase
{
    protected readonly IMafAiAgentRegistry LlmRegistry;
    protected async Task<string> CallLlmAsync(...)
    {
        var llm = await LlmRegistry.GetBestAgentAsync(scenario);
        return await llm.ExecuteAsync(...);
    }
}

// ❌ 错误：业务Agent继承AIAgent
// public class LightingAgent : AIAgent { }
```

### 3. 存储抽象可替换
```csharp
// Core层定义抽象
public interface ICacheStore { }
public interface IVectorStore { }
public interface IRelationalDatabase { }

// Infrastructure层实现
public class RedisCacheStore : ICacheStore { }
public class QdrantVectorStore : IVectorStore { }
public class EfCoreRelationalDatabase : IRelationalDatabase { }
```

---

## 🚀 快速开始 (2分钟上手)

### 1. 安装依赖
```bash
# 安装.NET 10 SDK
dotnet --version

# 克隆项目
git clone <repo-url>
cd CKY.MultiAgentFramework
```

### 2. 配置服务
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 🚀 一行注册所有内置服务
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 注册你的Agent
builder.Services.AddScoped<LightingAgent>();

var app = builder.Build();
app.Run();
```

### 3. 创建Agent
```csharp
public class LightingAgent : MafBusinessAgentBase
{
    public LightingAgent(IMafAiAgentRegistry llm, ICacheStore cache)
        : base(llm, cache) { }

    public override async Task<MafTaskResponse> ExecuteAsync(
        MafTaskRequest request,
        CancellationToken ct)
    {
        var intent = await CallLlmAsync(
            "你是意图识别助手",
            request.Text,
            LlmScenario.Intent);

        // ... 业务逻辑
        return new MafTaskResponse { Success = true };
    }
}
```

### 4. 配置文件
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "PostgreSQL": "Host=localhost;Database=mafdb"
  },
  "MafStorage": {
    "UseBuiltinImplementations": true
  }
}
```

---

## 📦 核心组件速查

### LLM服务 (7大提供商)
```csharp
// 智谱AI (首选)
ZhipuAIMafAiAgent

// 备选
TongyiMafAiAgent      // 通义千问
QwenMafAiAgent        // 通义千问
BaichuanMafAiAgent    // 百川
MiniMaxMafAiAgent     // MiniMax
SparkMafAiAgent       // 讯飞星火

// 自动降级
FallbackLlmAgent
```

### 存储服务
```csharp
// 三层缓存策略
IMemoryCache  (L1: 内存，最快)
ICacheStore   (L2: Redis，分布式)
IRelationalDatabase (L3: PostgreSQL，持久化)

// 向量存储
IVectorStore (Qdrant / MemoryVectorStore)
```

### 业务服务
```csharp
MafTaskScheduler       // 任务调度器
MafIntentRecognizer    // 意图识别器
MafTaskOrchestrator    // 任务编排器
DegradationManager     // 降级管理器
```

---

## 🛡️ 弹性策略

### 5级服务降级
| 级别 | 触发条件 | 降级措施 |
|------|---------|---------|
| 1 | Redis失败>20% | 禁用推荐系统 |
| 2 | LLM错误>30% | 禁用向量搜索 |
| 3 | DB连接池耗尽 | 仅用L1缓存 |
| 4 | LLM不可用 | 使用GLM-4-Air |
| 5 | 全部LLM不可用 | 规则引擎兜底 |

### 重试策略
```
指数退避 + 抖动
初始: 1000ms
倍数: 2
最大重试: 3次
抖动: ±200ms
```

### 熔断器
```
LLM:   10次失败/60秒 → 熔断120秒
Redis: 20次失败/30秒 → 熔断60秒
DB:    5次失败/60秒 → 熔断180秒
```

---

## 📊 监控指标

### Prometheus关键指标
```bash
# 业务指标
maf_task_total{status="success|failure"}
maf_llm_tokens_total{provider}
maf_cache_hit_rate{tier="l1|l2"}

# 基础设施
maf_redis_connection_pool_active
maf_db_connection_pool_active
maf_llm_latency_seconds{provider}
```

### 分布式追踪
```
OpenTelemetry自动追踪：
- Agent调用链路
- LLM请求链路
- 任务分解链路
- 数据库查询链路
```

---

## 🔧 常用命令

### 开发
```bash
# 构建
dotnet build CKY.MultiAgentFramework.sln

# 运行测试
dotnet test --collect:"XPlat Code Coverage"

# 运行Demo
dotnet run --project src/Demos/SmartHome

# 数据库迁移
dotnet ef migrations add <Name> --project src/Infrastructure/Repository
dotnet ef database update --project src/Infrastructure/Repository
```

### Docker
```bash
# 构建镜像
docker build -t cky-maf:latest .

# 运行完整栈
docker-compose up -d

# 查看日志
docker-compose logs -f smart-home
```

---

## 📚 文档导航

### 核心文档
- [架构设计](specs/00-CORE-ARCHITECTURE.md) - 完整架构说明
- [实现指南](specs/01-IMPLEMENTATION-GUIDE.md) - 代码实现细节

### 操作指南
- [LLM快速入门](guides/LLM_AGENT_QUICK_START.md)
- [集成LLM](guides/how-to-integrate-llm-with-agent-framework.md)
- [使用MafAiAgent](guides/how-to-use-mafaiagent.md)

### 历史文档
- [开发计划归档](archives/plans/) - 历史开发记录
- [架构决策归档](archives/decisions/) - 架构决策记录

---

## ⚡ 性能基准

### 响应时间目标
| 操作类型 | P50 | P95 | P99 |
|---------|-----|-----|-----|
| 简单任务 | <0.5s | <1s | <2s |
| 复杂任务 | <2s | <5s | <10s |
| LLM调用 | <1s | <3s | <5s |

### 资源限制
```
CPU: 正常<50%, 告警>80%
内存: 正常<500MB, 告警>1GB
GC暂停: 正常<50ms, 告警>100ms
```

---

## 🐛 常见问题

### Q1: 为什么业务Agent不继承AIAgent?
**A**: 业务Agent应该专注于业务逻辑，LLM调用通过组合`IMafAiAgentRegistry`实现，避免强制实现MS AF的抽象方法。

### Q2: 如何切换LLM提供商?
**A**: 修改数据库`LlmProviderConfig`表，或使用`LlmAgentFactory`动态创建。

### Q3: Core层可以依赖EF Core吗?
**A**: ❌ 不可以。Core层只能定义抽象接口(`IRelationalDatabase`)，EF Core实现在Infrastructure层。

### Q4: 如何禁用某个功能?
**A**: 使用降级管理器`DegradationManager`，或配置`IsEnabled=false`。

---

## 📞 获取帮助

- **文档**: [docs/](./)
- **示例**: [src/Demos/](../src/Demos/)
- **测试**: [tests/](../tests/)
- **问题**: [GitHub Issues](https://github.com/your-repo/issues)

---

**维护者**: CKY.MultiAgentFramework架构团队
**最后更新**: 2026-03-16
