# CKY.MAF.Infrastructure

Infrastructure 层实现 Core 层定义的所有存储抽象接口。

## 自动服务注册

使用 `AddMafInfrastructureServices` 扩展方法自动注册所有服务：

```csharp
services.AddMafInfrastructureServices(builder.Configuration);
```

## 支持的接口和实现

| 接口 | 默认实现 | 可选实现 | 生命周期 |
|------|----------|----------|----------|
| ICacheStore | MemoryCacheStore | RedisCacheStore | Singleton |
| IVectorStore | MemoryVectorStore | QdrantVectorStore | Singleton |
| IRelationalDatabase | EfCoreRelationalDatabase | - | Scoped |
| IMafAiSessionStore | DatabaseMafAiSessionStore | RedisMafAiSessionStore | Singleton/Scoped |

## 配置

通过 `appsettings.json` 配置实现选择：

```json
{
  "MafServices": {
    "Implementations": {
      "ICacheStore": "RedisCacheStore",
      "IVectorStore": "QdrantVectorStore"
    }
  }
}
```

**注意**：配置未指定时，使用默认实现（内存实现或 SQLite）。

## 项目结构

```
CKY.MAF.Infrastructure/
├── Caching/
│   ├── Memory/
│   │   └── MemoryCacheStore.cs          # 内存缓存实现
│   └── Redis/
│       └── RedisCacheStore.cs           # Redis 缓存实现
├── Vectorization/
│   ├── Memory/
│   │   └── MemoryVectorStore.cs         # 内存向量存储
│   └── Qdrant/
│       └── QdrantVectorStore.cs         # Qdrant 向量存储
├── Relational/
│   └── EntityFrameworkCore/
│       ├── EfCoreRelationalDatabase.cs  # EF Core 关系数据库
│       └── MafDbContext.cs              # DbContext
├── SessionStorage/
│   ├── Database/
│   │   └── DatabaseMafAiSessionStore.cs # 数据库会话存储
│   └── Redis/
│       └── RedisMafAiSessionStore.cs    # Redis 会话存储
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs   # 自动服务注册扩展
```

## 开发指南

### 添加新的存储实现

1. 在相应的子目录（Caching/Vectorization/Relational/SessionStorage）中创建新实现类
2. 实现对应的 Core 层接口
3. 在 `DependencyInjection/ServiceCollectionExtensions.cs` 中注册实现
4. 添加单元测试和集成测试

### 测试

- **单元测试**：使用 Moq 模拟依赖
- **集成测试**：使用 Testcontainers 运行真实服务（Redis、PostgreSQL、Qdrant）

## 依赖说明

- **Core**：定义的所有存储抽象接口
- **Microsoft.Extensions.DependencyInjection**：DI 容器
- **Microsoft.Extensions.Options**：配置选项
- **StackExchange.Redis**：Redis 客户端（可选）
- **Microsoft.EntityFrameworkCore**：ORM 框架
- **Microsoft.EntityFrameworkCore.Sqlite**：SQLite 提供程序（开发环境）
- **Qdrant.Client**：Qdrant 客户端（可选）
