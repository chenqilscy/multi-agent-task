# 自动服务注册功能设计文档

> **文档版本**: v1.0
> **创建日期**: 2026-03-14
> **设计原则**: 简单直接、零反射、配置驱动
> **状态**: ✅ 已批准

---

## 📋 目录

1. [概述](#概述)
2. [设计目标](#设计目标)
3. [核心设计](#核心设计)
4. [使用方式](#使用方式)
5. [配置说明](#配置说明)
6. [错误处理](#错误处理)
7. [测试策略](#测试策略)
8. [实现清单](#实现清单)

---

## 概述

为 CKY.MAF 框架添加自动服务注册功能，避免上层应用（Demo 应用）手工注册 Infrastructure 层的所有接口-实现映射。

### 设计理念

**简单直接**：不使用反射、扫描器、选择器等复杂组件，直接在扩展方法中硬编码 `services.AddXXX()` 调用。

**配置驱动**：代码中内置默认实现（内存实现），通过配置文件覆盖为生产环境实现。

**零依赖**：不引入任何第三方库（如 Scrutor），使用纯 .NET DI 容器 API。

---

## 设计目标

### 功能目标

✅ **自动注册**：一行代码注册所有 Infrastructure 层服务
✅ **默认内存实现**：开发环境无需配置即可运行
✅ **配置覆盖**：生产环境通过配置切换到真实实现
✅ **手动覆盖**：支持代码级覆盖（后注册覆盖先注册）

### 非功能目标

✅ **零反射**：性能最优
✅ **类型安全**：编译时检查
✅ **易于维护**：添加新服务只需几行代码
✅ **清晰透明**：逻辑一目了然

---

## 核心设计

### 架构图

```
┌─────────────────────────────────────────────────────────────┐
│  Demo 应用层                                                │
│  Program.cs                                                │
│                                                             │
│  services.AddMafInfrastructureServices(configuration);      │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  扩展方法层                                                  │
│  MafServiceRegistrationExtensions                          │
│  CKY.MAF.Infrastructure.DependencyInjection               │
│                                                             │
│  1. 读取配置文件                                              │
│  2. 判断配置值（null → 内存实现，有值 → 按配置注册）            │
│  3. 注册到 DI 容器                                           │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Infrastructure 层实现                                      │
│  Caching / Vectorization / Relational                      │
└─────────────────────────────────────────────────────────────┘
```

### 核心组件

#### 唯一文件：MafServiceRegistrationExtensions.cs

**位置**：`src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs`

**职责**：提供扩展方法，自动注册所有 Infrastructure 层服务

**核心逻辑**：
```csharp
public static IServiceCollection AddMafInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 对每个接口：
    // 1. 读取配置
    // 2. 判断：配置为空或为"MemoryXXX" → 注册内存实现
    // 3. 配置有值 → 按配置注册对应实现
    // 4. 配置无效 → 记录警告，使用默认实现

    return services;
}
```

### 注册逻辑流程

```
1. 读取配置: configuration["MafServices:Implementations:ICacheStore"]
   ↓
2. 判断配置值
   ├─ string.IsNullOrEmpty → 注册 MemoryCacheStore
   ├─ "MemoryCacheStore" → 注册 MemoryCacheStore
   ├─ "RedisCacheStore" → 注册 RedisCacheStore
   └─ 其他值 → 记录警告，注册 MemoryCacheStore
   ↓
3. 重复步骤 1-2 处理其他接口
   - IVectorStore
   - IRelationalDatabase
   - IMafAiSessionStore
   ↓
4. 返回 IServiceCollection
```

---

## 使用方式

### 基本用法

**Program.cs**：
```csharp
var builder = WebApplication.CreateBuilder(args);

// 一行代码注册所有 Infrastructure 服务
services.AddMafInfrastructureServices(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 手动覆盖

**Program.cs**（特殊需求时）：
```csharp
// 先自动注册
services.AddMafInfrastructureServices(builder.Configuration);

// 手动覆盖（后注册覆盖先注册）
services.AddSingleton<ICacheStore, CustomCacheStore>();
```

---

## 配置说明

### 配置文件结构

**appsettings.json**（可选）：
```json
{
  "MafServices": {
    "Implementations": {
      "ICacheStore": "MemoryCacheStore",
      "IVectorStore": "MemoryVectorStore"
    }
  }
}
```

### 开发环境配置

**appsettings.Development.json**：
```json
{
  "MafServices": {
    "Implementations": {
      // 不配置或注释掉 → 使用默认内存实现
    }
  }
}
```

### 生产环境配置

**appsettings.Production.json**：
```json
{
  "MafServices": {
    "Implementations": {
      "ICacheStore": "RedisCacheStore",
      "IVectorStore": "QdrantVectorStore",
      "IRelationalDatabase": "EfCoreRelationalDatabase",
      "IMafAiSessionStore": "RedisMafAiSessionStore"
    }
  }
}
```

### 配置优先级

1. **环境变量**（最高优先级）
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **代码默认值**（最低优先级）

---

## 错误处理

### 1. 配置值无效

**场景**：配置文件中指定了不存在的实现类名

**处理方式**：
- 记录警告日志
- 使用默认内存实现
- 应用继续运行

**日志输出示例**：
```
warn: CKY.MAF.Infrastructure.DependencyInjection.MafServiceRegistrationExtensions[0]
      Unknown implementation 'MemCacheStore' for ICacheStore.
      Using default 'MemoryCacheStore'. Valid values: MemoryCacheStore, RedisCacheStore
```

### 2. 必要依赖缺失

**场景**：选择了需要外部依赖的实现，但依赖未注册

**示例**：选择了 `RedisCacheStore` 但 `IConnectionMultiplexer` 未注册

**处理方式**：
- 不在扩展方法中处理
- 让应用启动时自然失败
- 在文档中提供清晰的错误消息和解决建议

### 3. 配置冲突

**场景**：同时配置了多个不兼容的实现

**处理方式**：
- 不检测冲突
- .NET DI 容器自然处理（后注册覆盖先注册）
- 在文档中说明优先级

---

## 测试策略

### 单元测试

**测试目标**：验证配置逻辑正确性

**测试用例**：
```csharp
public class MafServiceRegistrationExtensionsTests
{
    [Fact]
    public void AddMafInfrastructureServices_WithNoConfig_ShouldRegisterMemoryImplementations()
    {
        // 无配置 → 注册内存实现
    }

    [Fact]
    public void AddMafInfrastructureServices_WithRedisConfig_ShouldRegisterRedisCacheStore()
    {
        // 配置 Redis → 注册 RedisCacheStore
    }

    [Fact]
    public void AddMafInfrastructureServices_WithInvalidConfig_ShouldLogWarningAndUseDefault()
    {
        // 无效配置 → 记录警告，使用默认
    }
}
```

### 集成测试

**测试目标**：验证实际服务可用

**测试用例**：
```csharp
[Fact]
public async Task MemoryCacheStore_ShouldBeResolvable()
{
    // 验证可以从 DI 容器解析服务
    var cacheStore = serviceProvider.GetRequiredService<ICacheStore>();
    cacheStore.Should().BeOfType<MemoryCacheStore>();
}
```

---

## 实现清单

### Phase 1: 核心功能实现

- [ ] 创建 `src/Infrastructure/DependencyInjection/` 目录
- [ ] 实现 `MafServiceRegistrationExtensions.cs`
  - [ ] `AddMafInfrastructureServices` 扩展方法
  - [ ] ICacheStore 注册逻辑
  - [ ] IVectorStore 注册逻辑
  - [ ] IRelationalDatabase 注册逻辑
  - [ ] IMafAiSessionStore 注册逻辑
- [ ] 添加 XML 注释文档

### Phase 2: 错误处理

- [ ] 实现配置值无效时的警告日志
- [ ] 验证必要依赖检查（外部文档）

### Phase 3: 测试

- [ ] 创建单元测试项目
- [ ] 编写单元测试（3-5 个测试用例）
- [ ] 编写集成测试（2-3 个测试用例）

### Phase 4: 文档

- [ ] 更新 CLAUDE.md
- [ ] 添加使用示例到 README.md
- [ ] 添加配置说明

---

## 支持的服务映射

### 存储抽象接口

| 接口 | 默认实现 | 可选实现 | 生命周期 |
|------|----------|----------|----------|
| ICacheStore | MemoryCacheStore | RedisCacheStore | Singleton |
| IVectorStore | MemoryVectorStore | QdrantVectorStore | Singleton |
| IRelationalDatabase | InMemoryDatabase | EfCoreRelationalDatabase | Scoped |
| IMafAiSessionStore | DatabaseMafAiSessionStore | RedisMafAiSessionStore | Singleton/Scoped |

---

## 优势总结

✅ **超简单**：只有一个文件，逻辑一目了然
✅ **零反射**：性能最好，启动最快
✅ **易维护**：添加新服务只需加几行代码
✅ **类型安全**：编译时检查，无运行时错误
✅ **支持覆盖**：配置文件或手动注册均可
✅ **开发友好**：默认内存实现，无需配置
✅ **生产就绪**：配置文件切换到真实实现

---

**设计者**: Claude (Sonnet 4.6)
**审核者**: 待定
**最后更新**: 2026-03-14
