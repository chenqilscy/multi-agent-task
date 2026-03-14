# 自动服务注册功能实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 添加自动服务注册扩展方法，一行代码注册所有 Infrastructure 层服务，支持配置文件覆盖默认实现

**架构:** 创建单个扩展方法文件，硬编码所有接口-实现映射，从配置文件读取覆盖值，默认使用内存实现

**技术栈:** .NET 10, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Configuration

---

## Task 1: 创建项目目录结构

**Files:**
- Create: `src/Infrastructure/DependencyInjection/` 目录

**Step 1: 创建 DependencyInjection 目录**

Run: `mkdir -p "src/Infrastructure/DependencyInjection"`

Expected: 目录创建成功

**Step 2: 验证目录创建**

Run: `ls -la "src/Infrastructure/DependencyInjection"`

Expected: 目录存在且为空

**Step 3: Commit**

```bash
git add src/Infrastructure/DependencyInjection
git commit -m "feat: add DependencyInjection directory for auto service registration"
```

---

## Task 2: 实现核心扩展方法框架

**Files:**
- Create: `src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs`
- Reference: `docs/plans/2026-03-14-auto-service-registration-design.md`

**Step 1: 创建扩展方法文件框架**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.DependencyInjection;

/// <summary>
/// 自动服务注册扩展方法
/// </summary>
public static class MafServiceRegistrationExtensions
{
    /// <summary>
    /// 自动注册所有 Infrastructure 层服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddMafInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: 在后续任务中实现各个服务的注册逻辑

        return services;
    }
}
```

**Step 2: 编译验证**

Run: `dotnet build src/Infrastructure/CKY.MAF.Infrastructure.csproj`

Expected: 编译成功

**Step 3: Commit**

```bash
git add src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs
git commit -m "feat: add MafServiceRegistrationExtensions framework"
```

---

## Task 3: 实现 ICacheStore 注册逻辑

**Files:**
- Modify: `src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs`
- Reference: `src/Infrastructure/Caching/Memory/MemoryCacheStore.cs`
- Reference: `src/Infrastructure/Caching/Redis/RedisCacheStore.cs`

**Step 1: 添加 ICacheStore 注册逻辑**

在 `AddMafInfrastructureServices` 方法中添加：

```csharp
// ========================================
// 缓存服务注册
// ========================================
var cacheImpl = configuration["MafServices:Implementations:ICacheStore"];

if (string.IsNullOrEmpty(cacheImpl) || cacheImpl == "MemoryCacheStore")
{
    // 配置未指定或指定内存实现 → 使用内存实现
    services.AddSingleton<ICacheStore, MemoryCacheStore>();
}
else if (cacheImpl == "RedisCacheStore")
{
    services.AddSingleton<ICacheStore, RedisCacheStore>();
}
else
{
    // 配置值无效，记录警告并使用默认实现
    var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
    loggerFactory?.CreateLogger<MafServiceRegistrationExtensions>()
        .LogWarning("Unknown implementation '{Implementation}' for ICacheStore. " +
                    "Using default 'MemoryCacheStore'. Valid values: MemoryCacheStore, RedisCacheStore",
                    cacheImpl);

    services.AddSingleton<ICacheStore, MemoryCacheStore>();
}
```

**Step 2: 添加必要的 using 语句**

在文件顶部添加：
```csharp
using CKY.MultiAgentFramework.Core.Abstractions.Interfaces;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
```

**Step 3: 编译验证**

Run: `dotnet build src/Infrastructure/CKY.MAF.Infrastructure.csproj`

Expected: 编译成功

**Step 4: Commit**

```bash
git add src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs
git commit -m "feat: implement ICacheStore auto-registration"
```

---

## Task 4: 实现 IVectorStore 注册逻辑

**Files:**
- Modify: `src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs`
- Reference: `src/Infrastructure/Vectorization/Memory/MemoryVectorStore.cs`
- Reference: `src/Infrastructure/Vectorization/Qdrant/QdrantVectorStore.cs`

**Step 1: 添加 IVectorStore 注册逻辑**

在 `AddMafInfrastructureServices` 方法中，ICacheStore 逻辑之后添加：

```csharp
// ========================================
// 向量存储服务注册
// ========================================
var vectorImpl = configuration["MafServices:Implementations:IVectorStore"];

if (string.IsNullOrEmpty(vectorImpl) || vectorImpl == "MemoryVectorStore")
{
    // 默认: 内存实现
    services.AddSingleton<IVectorStore, MemoryVectorStore>();
}
else if (vectorImpl == "QdrantVectorStore")
{
    services.AddSingleton<IVectorStore, QdrantVectorStore>();
}
else
{
    // 配置值无效，记录警告并使用默认实现
    var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
    loggerFactory?.CreateLogger<MafServiceRegistrationExtensions>()
        .LogWarning("Unknown implementation '{Implementation}' for IVectorStore. " +
                    "Using default 'MemoryVectorStore'. Valid values: MemoryVectorStore, QdrantVectorStore",
                    vectorImpl);

    services.AddSingleton<IVectorStore, MemoryVectorStore>();
}
```

**Step 2: 添加必要的 using 语句**

在文件顶部添加：
```csharp
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant;
```

**Step 3: 编译验证**

Run: `dotnet build src/Infrastructure/CKY.MAF.Infrastructure.csproj`

Expected: 编译成功

**Step 4: Commit**

```bash
git add src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs
git commit -m "feat: implement IVectorStore auto-registration"
```

---

## Task 5: 实现 IRelationalDatabase 注册逻辑

**Files:**
- Modify: `src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs`
- Reference: `src/Infrastructure/Relational/EfCoreRelationalDatabase.cs` (需确认实际路径)

**Step 1: 添加 IRelationalDatabase 注册逻辑**

在 `AddMafInfrastructureServices` 方法中，IVectorStore 逻辑之后添加：

```csharp
// ========================================
// 关系数据库服务注册
// ========================================
var dbImpl = configuration["MafServices:Implementations:IRelationalDatabase"];

if (string.IsNullOrEmpty(dbImpl) || dbImpl == "InMemoryDatabase")
{
    // 默认: 内存数据库
    services.AddScoped<IRelationalDatabase, InMemoryDatabase>();
}
else if (dbImpl == "EfCoreRelationalDatabase")
{
    services.AddScoped<IRelationalDatabase, EfCoreRelationalDatabase>();
}
else
{
    // 配置值无效，记录警告并使用默认实现
    var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
    loggerFactory?.CreateLogger<MafServiceRegistrationExtensions>()
        .LogWarning("Unknown implementation '{Implementation}' for IRelationalDatabase. " +
                    "Using default 'InMemoryDatabase'. Valid values: InMemoryDatabase, EfCoreRelationalDatabase",
                    dbImpl);

    services.AddScoped<IRelationalDatabase, InMemoryDatabase>();
}
```

**注意**: 需要先确认 `IRelationalDatabase` 接口和实际实现类的路径，如果不存在则跳过此任务。

**Step 2: 编译验证**

Run: `dotnet build src/Infrastructure/CKY.MAF.Infrastructure.csproj`

Expected: 编译成功（如果接口/实现存在）

**Step 3: Commit**

```bash
git add src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs
git commit -m "feat: implement IRelationalDatabase auto-registration"
```

---

## Task 6: 实现 IMafAiSessionStore 注册逻辑

**Files:**
- Modify: `src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs`
- Reference: `src/Infrastructure/Caching/Redis/RedisMafAiSessionStore.cs`
- Reference: `src/Infrastructure/Relational/DatabaseMafAiSessionStore.cs`

**Step 1: 添加 IMafAiSessionStore 注册逻辑**

在 `AddMafInfrastructureServices` 方法中，IRelationalDatabase 逻辑之后添加：

```csharp
// ========================================
// Session 存储服务注册
// ========================================
var sessionImpl = configuration["MafServices:Implementations:IMafAiSessionStore"];

if (string.IsNullOrEmpty(sessionImpl) || sessionImpl == "DatabaseMafAiSessionStore")
{
    // 默认: 数据库实现（因为需要 DbContext）
    services.AddScoped<IMafAiSessionStore, DatabaseMafAiSessionStore>();
}
else if (sessionImpl == "RedisMafAiSessionStore")
{
    services.AddSingleton<IMafAiSessionStore, RedisMafAiSessionStore>();
}
else
{
    // 配置值无效，记录警告并使用默认实现
    var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
    loggerFactory?.CreateLogger<MafServiceRegistrationExtensions>()
        .LogWarning("Unknown implementation '{Implementation}' for IMafAiSessionStore. " +
                    "Using default 'DatabaseMafAiSessionStore'. Valid values: DatabaseMafAiSessionStore, RedisMafAiSessionStore",
                    sessionImpl);

    services.AddScoped<IMafAiSessionStore, DatabaseMafAiSessionStore>();
}
```

**Step 2: 添加必要的 using 语句**

在文件顶部添加：
```csharp
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using CKY.MultiAgentFramework.Infrastructure.Relational;
```

**Step 3: 编译验证**

Run: `dotnet build src/Infrastructure/CKY.MAF.Infrastructure.csproj`

Expected: 编译成功

**Step 4: Commit**

```bash
git add src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs
git commit -m "feat: implement IMafAiSessionStore auto-registration"
```

---

## Task 7: 创建单元测试项目

**Files:**
- Create: `src/tests/UnitTests/Infrastructure/DependencyInjection/CKY.MAF.UnitTests.DependencyInjection.csproj`
- Create: `src/tests/UnitTests/Infrastructure/DependencyInjection/MafServiceRegistrationExtensionsTests.cs`

**Step 1: 创建测试项目文件**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../../Infrastructure/CKY.MAF.Infrastructure.csproj" />
    <ProjectReference Include="../../../../../Core/CKY.MAF.Core.csproj" />
  </ItemGroup>

</Project>
```

**Step 2: 创建测试类框架**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CKY.MultiAgentFramework.Infrastructure.DependencyInjection;

public class MafServiceRegistrationExtensionsTests
{
    [Fact]
    public void AddMafInfrastructureServices_WithNoConfig_ShouldRegisterMemoryImplementations()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddMafInfrastructureServices(configuration);

        // Assert
        services.Should().ContainSingle(
            sd => sd.ServiceType == typeof(ICacheStore) &&
                  sd.ImplementationType == typeof(MemoryCacheStore));
    }
}
```

**Step 3: 编译验证**

Run: `dotnet build src/tests/UnitTests/Infrastructure/DependencyInjection/CKY.MAF.UnitTests.DependencyInjection.csproj`

Expected: 编译成功

**Step 4: Commit**

```bash
git add src/tests/UnitTests/Infrastructure/DependencyInjection/
git commit -m "test: add unit test project for auto service registration"
```

---

## Task 8: 实现无配置场景测试

**Files:**
- Modify: `src/tests/UnitTests/Infrastructure/DependencyInjection/MafServiceRegistrationExtensionsTests.cs`

**Step 1: 添加测试方法**

```csharp
[Fact]
public void AddMafInfrastructureServices_WithNoConfig_ShouldRegisterMemoryImplementations()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>())
        .Build();

    // Act
    services.AddMafInfrastructureServices(configuration);

    // Assert - 验证所有服务都注册了内存实现
    var cacheStoreDescriptor = services.FirstOrDefault(
        sd => sd.ServiceType == typeof(ICacheStore));
    cacheStoreDescriptor.Should().NotBeNull();
    cacheStoreDescriptor?.ImplementationType.Should().Be(typeof(MemoryCacheStore));
    cacheStoreDescriptor?.Lifetime.Should().Be(ServiceLifetime.Singleton);

    var vectorStoreDescriptor = services.FirstOrDefault(
        sd => sd.ServiceType == typeof(IVectorStore));
    vectorStoreDescriptor.Should().NotBeNull();
    vectorStoreDescriptor?.ImplementationType.Should().Be(typeof(MemoryVectorStore));
}
```

**Step 2: 运行测试**

Run: `dotnet test src/tests/UnitTests/Infrastructure/DependencyInjection/CKY.MAF.UnitTests.DependencyInjection.csproj --filter "FullyQualifiedName~WithNoConfig"`

Expected: PASS

**Step 3: Commit**

```bash
git add src/tests/UnitTests/Infrastructure/DependencyInjection/MafServiceRegistrationExtensionsTests.cs
git commit -m "test: add test for default memory implementations"
```

---

## Task 9: 实现有配置场景测试

**Files:**
- Modify: `src/tests/UnitTests/Infrastructure/DependencyInjection/MafServiceRegistrationExtensionsTests.cs`

**Step 1: 添加测试方法**

```csharp
[Fact]
public void AddMafInfrastructureServices_WithRedisConfig_ShouldRegisterRedisCacheStore()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MafServices:Implementations:ICacheStore"] = "RedisCacheStore"
        })
        .Build();

    // Act
    services.AddMafInfrastructureServices(configuration);

    // Assert
    var descriptor = services.FirstOrDefault(
        sd => sd.ServiceType == typeof(ICacheStore));
    descriptor.Should().NotBeNull();
    descriptor?.ImplementationType.Should().Be(typeof(RedisCacheStore));
}
```

**Step 2: 运行测试**

Run: `dotnet test src/tests/UnitTests/Infrastructure/DependencyInjection/CKY.MAF.UnitTests.DependencyInjection.csproj --filter "FullyQualifiedName~WithRedisConfig"`

Expected: PASS

**Step 3: Commit**

```bash
git add src/tests/UnitTests/Infrastructure/DependencyInjection/MafServiceRegistrationExtensionsTests.cs
git commit -m "test: add test for Redis configuration override"
```

---

## Task 10: 实现无效配置场景测试

**Files:**
- Modify: `src/tests/UnitTests/Infrastructure/DependencyInjection/MafServiceRegistrationExtensionsTests.cs`

**Step 1: 添加测试方法**

```csharp
[Fact]
public void AddMafInfrastructureServices_WithInvalidConfig_ShouldLogWarningAndUseDefault()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MafServices:Implementations:ICacheStore"] = "InvalidCacheStore"
        })
        .Build();

    // Act & Assert
    var action = () => services.AddMafInfrastructureServices(configuration);

    // 应该不抛出异常，而是使用默认实现
    action.Should().NotThrow();

    var descriptor = services.FirstOrDefault(
        sd => sd.ServiceType == typeof(ICacheStore));
    descriptor.Should().NotBeNull();
    descriptor?.ImplementationType.Should().Be(typeof(MemoryCacheStore));
}
```

**Step 2: 运行测试**

Run: `dotnet test src/tests/UnitTests/Infrastructure/DependencyInjection/CKY.MAF.UnitTests.DependencyInjection.csproj --filter "FullyQualifiedName~WithInvalidConfig"`

Expected: PASS

**Step 3: Commit**

```bash
git add src/tests/UnitTests/Infrastructure/DependencyInjection/MafServiceRegistrationExtensionsTests.cs
git commit -m "test: add test for invalid configuration handling"
```

---

## Task 11: 更新 Demo 应用使用新扩展方法

**Files:**
- Modify: `src/Demos/SmartHome/Program.cs`
- Reference: `src/Demos/SmartHome/Program.cs:17-174`

**Step 1: 替换手动注册为自动注册**

将 Program.cs 中的手动注册代码：

```csharp
// 删除或注释掉这些手动注册：
// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// builder.Services.AddScoped<IMainTaskRepository, MainTaskRepository>();
// builder.Services.AddScoped<ISubTaskRepository, SubTaskRepository>();
```

替换为：

```csharp
// 自动注册所有 Infrastructure 服务
services.AddMafInfrastructureServices(builder.Configuration);
```

**注意**: 只替换 Infrastructure 层的服务注册，保留其他业务服务的注册。

**Step 2: 添加必要的 using 语句**

在 Program.cs 顶部添加：
```csharp
using CKY.MultiAgentFramework.Infrastructure.DependencyInjection;
```

**Step 3: 编译验证**

Run: `dotnet build src/Demos/SmartHome/CKY.MAF.Demos.SmartHome.csproj`

Expected: 编译成功

**Step 4: Commit**

```bash
git add src/Demos/SmartHome/Program.cs
git commit -m "refactor: use auto service registration in SmartHome demo"
```

---

## Task 12: 更新文档

**Files:**
- Modify: `CLAUDE.md`
- Create: `src/Infrastructure/README.md` (如果不存在)

**Step 1: 更新 CLAUDE.md**

在 CLAUDE.md 的 "Common Tasks" 部分添加：

```markdown
### 注册 Infrastructure 服务

使用自动注册扩展方法：

```csharp
// Program.cs
services.AddMafInfrastructureServices(builder.Configuration);
```

**默认实现**（开发环境）：
- ICacheStore → MemoryCacheStore
- IVectorStore → MemoryVectorStore
- IRelationalDatabase → InMemoryDatabase
- IMafAiSessionStore → DatabaseMafAiSessionStore

**配置覆盖**（生产环境）：

在 `appsettings.Production.json` 中配置：
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
```

**Step 2: 创建 Infrastructure README**

创建 `src/Infrastructure/README.md`：

```markdown
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
| IRelationalDatabase | InMemoryDatabase | EfCoreRelationalDatabase | Scoped |
| IMafAiSessionStore | DatabaseMafAiSessionStore | RedisMafAiSessionStore | Singleton/Scoped |

## 配置

通过 `appsettings.json` 配置实现选择：

```json
{
  "MafServices": {
    "Implementations": {
      "ICacheStore": "RedisCacheStore"
    }
  }
}
```
```

**Step 3: Commit**

```bash
git add CLAUDE.md src/Infrastructure/README.md
git commit -m "docs: add auto service registration documentation"
```

---

## Task 13: 运行所有测试验证

**Files:**
- Reference: 所有已创建的测试文件

**Step 1: 运行所有单元测试**

Run: `dotnet test src/tests/UnitTests/Infrastructure/DependencyInjection/CKY.MAF.UnitTests.DependencyInjection.csproj`

Expected: 所有测试 PASS

**Step 2: 运行 Demo 应用验证**

Run: `dotnet run --project src/Demos/SmartHome/CKY.MAF.Demos.SmartHome.csproj`

Expected: 应用启动成功，无错误

**Step 3: 验证日志输出**

检查控制台输出，确认：
- 无异常
- 服务正确注册
- 如有配置错误，应有警告日志

**Step 4: Commit 完成标记**

```bash
git commit --allow-empty -m "feat: complete auto service registration implementation

- Implemented MafServiceRegistrationExtensions.AddMafInfrastructureServices
- Supports 4 storage interfaces with configuration override
- Default: memory implementations for development
- Production: override via appsettings.json
- Added comprehensive unit tests
- Updated documentation

See design doc: docs/plans/2026-03-14-auto-service-registration-design.md"
```

---

## 完成检查清单

- [ ] 所有任务已完成
- [ ] 所有测试通过
- [ ] Demo 应用成功运行
- [ ] 文档已更新
- [ ] 代码已提交
- [ ] 设计文档和实现文档已保存

---

**实现者注意事项:**

1. **TDD**: 每个功能先写测试，再写实现
2. **小步提交**: 每完成一个小任务立即提交
3. **验证路径**: Task 5-6 需要先确认接口/实现类的实际路径
4. **依赖处理**: 如果某些实现类不存在，跳过对应任务并记录
5. **日志测试**: 警告日志测试可以简化为验证不抛异常
6. **配置优先级**: 配置未指定时使用内存实现，有值时按配置注册

**相关文档:**
- 设计文档: `docs/plans/2026-03-14-auto-service-registration-design.md`
- 实现计划: 本文档
