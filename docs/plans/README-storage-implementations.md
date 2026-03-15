# CKY.MAF 存储实现指南

本文档说明 CKY.MAF 的存储架构和内置实现。

## 架构原则

CKY.MAF 采用**渐进式混合架构**：
- ✅ 保留所有接口抽象（ICacheStore、IVectorStore、IRelationalDatabase）
- ✅ 提供内置推荐实现
- ✅ 通过文档和配置明确推荐方案
- ✅ 生产环境可按需切换

## 内置实现对比

| 接口 | 默认实现 | 生产环境推荐 | 部署复杂度 |
|------|---------|-------------|-----------|
| ICacheStore | RedisCacheStore | RedisCacheStore | 🟡 中等（需要 Redis） |
| IVectorStore | MemoryVectorStore | QdrantVectorStore | 🟢 简单（Demo）/ 🟡 中等（生产） |
| IRelationalDatabase | EfCoreRelationalDatabase (SQLite) | PostgreSQL | 🟢 简单（Demo）/ 🟡 中等（生产） |

## 快速启动

### Demo 应用（零配置）

```csharp
builder.Services.AddMafBuiltinServices(builder.Configuration);
```

配置文件：
```json
{
  "MafStorage": {
    "UseBuiltinImplementations": true
  }
}
```

### 生产环境

配置文件：
```json
{
  "ConnectionStrings": {
    "Redis": "redis-server:6379",
    "PostgreSQL": "Host=postgres;Database=mafdb"
  },
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  }
}
```

## 实现详解

### RedisCacheStore

**特性**：
- 分布式缓存，支持多实例部署
- 高性能键值存储（毫秒级响应）
- 支持过期时间、批量操作

**部署要求**：
- Redis 服务（Docker 或本地安装）
- 配置：`ConnectionStrings:Redis`

**降级策略**：
- Redis 不可用时，记录警告但不影响应用启动
- 缓存操作会失败，但核心功能可用

### MemoryVectorStore

**特性**：
- 零配置，开箱即用
- 适合 Demo 和开发测试
- 不持久化，重启丢失数据

**适用场景**：
- Demo 应用
- 开发测试环境
- 小规模场景（< 1万向量）

**限制**：
- 不持久化
- 不适合生产环境

### QdrantVectorStore（生产推荐）

**特性**：
- 专业向量数据库
- 高性能 HNSW 算法
- 持久化存储、可扩展

**部署要求**：
- Docker 部署 Qdrant
- 配置：`Qdrant:Host`

**适用场景**：
- 生产环境
- 大规模场景（> 10万向量）

### EfCoreRelationalDatabase

**支持数据库**：
- SQLite（默认）：文件数据库，零配置
- PostgreSQL（生产）：企业级数据库

**配置方式**：
```json
{
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "SQLite"  // 或 "PostgreSQL"
    }
  }
}
```

## 故障排查

### Redis 连接失败

**错误信息**：
```
Failed to connect to Redis at localhost:6379
```

**解决方案**：
1. 检查 Redis 服务是否启动
2. 验证连接字符串配置
3. 如果是 Demo 环境，可以忽略（缓存功能降级）

### SQLite 数据库文件锁定

**错误信息**：
```
database is locked
```

**解决方案**：
1. 确保只有一个应用实例访问数据库
2. 使用 WAL 模式（PRAGMA journal_mode=WAL）
3. 生产环境切换到 PostgreSQL

## 参考资料

- [架构文档](../specs/12-layered-architecture.md)
- [接口设计规范](../specs/06-interface-design-spec.md)
- [CLAUDE.md](../../CLAUDE.md)
