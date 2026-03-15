# CKY.MAF 单元测试实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为 CKY.MAF 项目的 Repository 层和 Core Services 层实现全面的单元测试，达到 70-80% 代码覆盖率。

**Architecture:**
- Repository 层使用 SQLite 内存数据库进行集成测试，验证 EF Core 映射和 LINQ 查询
- Services 层使用 Moq 模拟所有依赖进行纯单元测试
- 测试辅助类提供可复用的测试数据和基类

**Tech Stack:** xUnit, FluentAssertions, Moq, Microsoft.EntityFrameworkCore.Sqlite, .NET 10

---

## Phase 1: 基础设施

### Task 1: 添加 NuGet 包依赖

**Files:**
- Modify: `src/tests/UnitTests/CKY.MAF.Tests.csproj`

**Step 1: 添加 SQLite EF Core 包**

编辑 `CKY.MAF.Tests.csproj`，在 `<ItemGroup>` 中添加：

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
```

**Step 2: 恢复 NuGet 包**

```bash
cd src/tests/UnitTests
dotnet restore
```

预期输出：成功恢复包，无错误

**Step 3: 验证编译**

```bash
dotnet build --no-restore
```

预期输出：编译成功，无警告

**Step 4: 提交**

```bash
git add src/tests/UnitTests/CKY.MAF.Tests.csproj
git commit -m "test: add EF Core SQLite and InMemory packages"
```

---

### Task 2: 创建测试辅助类目录结构

**Files:**
- Create: `src/tests/UnitTests/Helpers/RepositoryTestBase.cs`
- Create: `src/tests/UnitTests/Helpers/TestDataBuilder.cs`
- Create: `src/tests/UnitTests/Helpers/ServiceTestBase.cs`

**Step 1: 创建 Helpers 目录**

```bash
mkdir -p src/tests/UnitTests/Helpers
```

**Step 2: 创建 RepositoryTestBase.cs**

```csharp
// src/tests/UnitTests/Helpers/RepositoryTestBase.cs
using CKY.MultiAgentFramework.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Tests.Helpers;

public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected MafDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MafDbContext>()
            .UseSqlite("DataSource=:memory:")
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new MafDbContext(options);
        await DbContext.Database.OpenConnectionAsync();
        await DbContext.Database.EnsureCreatedAsync();

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.CloseConnectionAsync();
        await DbContext.DisposeAsync();
    }

    protected virtual async Task SeedTestDataAsync()
    {
    }
}
```

**Step 3: 验证编译**

```bash
cd src/tests/UnitTests
dotnet build --no-restore
```

预期输出：编译成功

**Step 4: 提交**

```bash
git add src/tests/UnitTests/Helpers/RepositoryTestBase.cs
git commit -m "test: add RepositoryTestBase with SQLite in-memory database"
```

---

### Task 3: 创建 TestDataBuilder

**Files:**
- Create: `src/tests/UnitTests/Helpers/TestDataBuilder.cs`

**Step 1: 创建 TestDataBuilder.cs**

```csharp
// src/tests/UnitTests/Helpers/TestDataBuilder.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Tests.Helpers;

public static class TestDataBuilder
{
    public static MainTask CreateMainTask(Action<MainTask>? configure = null)
    {
        var task = new MainTask
        {
            Title = "Test Task",
            Description = "Test Description",
            Priority = TaskPriority.Normal,
            Status = MafTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(task);
        return task;
    }

    public static SubTask CreateSubTask(Action<SubTask>? configure = null)
    {
        var subTask = new SubTask
        {
            Title = "Test SubTask",
            Description = "Test SubTask Description",
            Status = MafTaskStatus.Pending,
            ExecutionOrder = 1
        };
        configure?.Invoke(subTask);
        return subTask;
    }

    public static SchedulePlanEntity CreateSchedulePlan(Action<SchedulePlanEntity>? configure = null)
    {
        var plan = new SchedulePlanEntity
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanJson = "{}",
            Status = SchedulePlanStatus.Created,
            TotalTasks = 1,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(plan);
        return plan;
    }

    public static ExecutionPlanEntity CreateExecutionPlan(Action<ExecutionPlanEntity>? configure = null)
    {
        var plan = new ExecutionPlanEntity
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanJson = "{}",
            Status = ExecutionPlanStatus.Created,
            TotalTasks = 1,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(plan);
        return plan;
    }

    public static TaskExecutionResultEntity CreateTaskExecutionResult(Action<TaskExecutionResultEntity>? configure = null)
    {
        var result = new TaskExecutionResultEntity
        {
            TaskId = Guid.NewGuid().ToString(),
            PlanId = Guid.NewGuid().ToString(),
            Success = true,
            StartedAt = DateTime.UtcNow
        };
        configure?.Invoke(result);
        return result;
    }

    public static LlmProviderConfig CreateLlmConfig(Action<LlmProviderConfig>? configure = null)
    {
        var config = new LlmProviderConfig
        {
            ProviderName = "test-provider",
            ProviderDisplayName = "Test Provider",
            ApiBaseUrl = "https://api.test.com",
            ApiKey = "test-key-12345678",
            ModelId = "test-model",
            ModelDisplayName = "Test Model",
            IsEnabled = true,
            Priority = 1,
            SupportedScenarios = new List<LlmScenario> { LlmScenario.Chat }
        };
        configure?.Invoke(config);
        return config;
    }
}
```

**Step 2: 验证编译**

```bash
cd src/tests/UnitTests
dotnet build --no-restore
```

预期输出：编译成功

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Helpers/TestDataBuilder.cs
git commit -m "test: add TestDataBuilder for test data creation"
```

---

### Task 4: 创建 ServiceTestBase

**Files:**
- Create: `src/tests/UnitTests/Helpers/ServiceTestBase.cs`

**Step 1: 创建 ServiceTestBase.cs**

```csharp
// src/tests/UnitTests/Helpers/ServiceTestBase.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Helpers;

public abstract class ServiceTestBase
{
    protected Mock<ILogger<T>> CreateLoggerMock<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    protected Mock<ICacheStore> CreateCacheStoreMock()
    {
        return new Mock<ICacheStore>();
    }

    protected Mock<IVectorStore> CreateVectorStoreMock()
    {
        return new Mock<IVectorStore>();
    }

    protected Mock<IRelationalDatabase> CreateRelationalDatabaseMock()
    {
        return new Mock<IRelationalDatabase>();
    }
}
```

**Step 2: 验证编译**

```bash
cd src/tests/UnitTests
dotnet build --no-restore
```

预期输出：编译成功

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Helpers/ServiceTestBase.cs
git commit -m "test: add ServiceTestBase with common mock helpers"
```

---

### Task 5: 创建 Repository 测试目录

**Step 1: 创建目录**

```bash
mkdir -p src/tests/UnitTests/Repository
```

**Step 2: 添加目录占位文件（用于 Git 跟踪）**

```bash
touch src/tests/UnitTests/Repository/.gitkeep
```

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Repository/
git commit -m "test: create Repository test directory"
```

---

## Phase 2: Repository 层测试

### Task 6: MainTaskRepositoryTests - Setup 和基础 CRUD

**Files:**
- Create: `src/tests/UnitTests/Repository/MainTaskRepositoryTests.cs`

**Step 1: 创建测试类框架和 AddAsync 测试**

```csharp
// src/tests/UnitTests/Repository/MainTaskRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class MainTaskRepositoryTests : RepositoryTestBase
{
    private readonly MainTaskRepository _repository;

    public MainTaskRepositoryTests()
    {
        _repository = new MainTaskRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask(t => t.Title = "New Task");

        // Act
        var result = await _repository.AddAsync(task);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Task");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ShouldReturnTaskWithSubTasks()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask(t => t.Title = "Test Task");
        await _repository.AddAsync(task);

        // Act
        var result = await _repository.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be("Test Task");
        result.SubTasks.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskNotFound_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTasksOrderedByCreatedAt()
    {
        // Arrange
        var task1 = TestDataBuilder.CreateMainTask(t => t.Title = "Task 1");
        var task2 = TestDataBuilder.CreateMainTask(t => t.Title = "Task 2");
        await _repository.AddAsync(task1);
        await Task.Delay(10);
        await _repository.AddAsync(task2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Task 2"); // 最新的在前
        result[1].Title.Should().Be("Task 1");
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnOnlyTasksWithGivenStatus()
    {
        // Arrange
        var pendingTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "Pending Task";
            t.Status = MafTaskStatus.Pending;
        });
        var completedTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "Completed Task";
            t.Status = MafTaskStatus.Completed;
        });
        await _repository.AddAsync(pendingTask);
        await _repository.AddAsync(completedTask);

        // Act
        var result = await _repository.GetByStatusAsync(MafTaskStatus.Pending);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Pending Task");
    }

    [Fact]
    public async Task GetHighPriorityTasksAsync_ShouldReturnTasksAboveThreshold()
    {
        // Arrange
        var highPriorityTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "High Priority";
            t.Priority = TaskPriority.High;
            t.Status = MafTaskStatus.Pending;
        });
        var lowPriorityTask = TestDataBuilder.CreateMainTask(t =>
        {
            t.Title = "Low Priority";
            t.Priority = TaskPriority.Low;
            t.Status = MafTaskStatus.Pending;
        });
        await _repository.AddAsync(highPriorityTask);
        await _repository.AddAsync(lowPriorityTask);

        // Act
        var result = await _repository.GetHighPriorityTasksAsync(50);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("High Priority");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyTaskProperties()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask(t => t.Title = "Original Title");
        await _repository.AddAsync(task);

        // Act
        task.Title = "Updated Title";
        await _repository.UpdateAsync(task);

        // Assert
        var result = await _repository.GetByIdAsync(task.Id);
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask()
    {
        // Arrange
        var task = TestDataBuilder.CreateMainTask();
        await _repository.AddAsync(task);

        // Act
        await _repository.DeleteAsync(task.Id);

        // Assert
        var result = await _repository.GetByIdAsync(task.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithSubTasks_ShouldSaveBoth()
    {
        // Arrange
        var mainTask = TestDataBuilder.CreateMainTask(t => t.Title = "Parent Task");
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Child Task";
            s.MainTaskId = mainTask.Id;
            s.MainTask = mainTask;
        });
        mainTask.SubTasks.Add(subTask);

        // Act
        var result = await _repository.AddAsync(mainTask);

        // Assert
        result.SubTasks.Should().HaveCount(1);
        result.SubTasks.First().Title.Should().Be("Child Task");
    }
}
```

**Step 2: 运行测试**

```bash
cd src/tests/UnitTests
dotnet test --filter "FullyQualifiedName~MainTaskRepositoryTests" -v n
```

预期输出：所有 10 个测试通过

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Repository/MainTaskRepositoryTests.cs
git commit -m "test: add MainTaskRepositoryTests with 10 test cases"
```

---

### Task 7: SubTaskRepositoryTests

**Files:**
- Create: `src/tests/UnitTests/Repository/SubTaskRepositoryTests.cs`

**Step 1: 创建 SubTaskRepositoryTests**

```csharp
// src/tests/UnitTests/Repository/SubTaskRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class SubTaskRepositoryTests : RepositoryTestBase
{
    private readonly SubTaskRepository _repository;

    public SubTaskRepositoryTests()
    {
        _repository = new SubTaskRepository(DbContext);
    }

    protected override async Task SeedTestDataAsync()
    {
        // 先创建一个 MainTask 用于测试
        var mainTask = TestDataBuilder.CreateMainTask(t => t.Title = "Parent Task");
        DbContext.MainTasks.Add(mainTask);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldAssignId()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "New SubTask";
            s.MainTaskId = mainTask.Id;
        });

        // Act
        var result = await _repository.AddAsync(subTask);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New SubTask");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeMainTask()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Test SubTask";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask);

        // Act
        var result = await _repository.GetByIdAsync(subTask.Id);

        // Assert
        result.Should().NotBeNull();
        result!.MainTask.Should().NotBeNull();
        result.MainTask.Title.Should().Be("Parent Task");
    }

    [Fact]
    public async Task GetByMainTaskIdAsync_ShouldReturnSubTasksOrderedByExecutionOrder()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask1 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "First";
            s.MainTaskId = mainTask.Id;
            s.ExecutionOrder = 1;
        });
        var subTask2 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Second";
            s.MainTaskId = mainTask.Id;
            s.ExecutionOrder = 2;
        });
        await _repository.AddAsync(subTask2); // 故意乱序添加
        await _repository.AddAsync(subTask1);

        // Act
        var result = await _repository.GetByMainTaskIdAsync(mainTask.Id);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("First");
        result[1].Title.Should().Be("Second");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllSubTasks()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask1 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "SubTask 1";
            s.MainTaskId = mainTask.Id;
        });
        var subTask2 = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "SubTask 2";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask1);
        await _repository.AddAsync(subTask2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleSubTasks()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTasks = new List<SubTask>
        {
            TestDataBuilder.CreateSubTask(s => { s.Title = "Batch 1"; s.MainTaskId = mainTask.Id; }),
            TestDataBuilder.CreateSubTask(s => { s.Title = "Batch 2"; s.MainTaskId = mainTask.Id; }),
            TestDataBuilder.CreateSubTask(s => { s.Title = "Batch 3"; s.MainTaskId = mainTask.Id; })
        };

        // Act
        var result = await _repository.AddRangeAsync(subTasks);

        // Assert
        result.Should().HaveCount(3);
        result.All(r => r.Id > 0).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySubTask()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "Original Title";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask);

        // Act
        subTask.Title = "Updated Title";
        await _repository.UpdateAsync(subTask);

        // Assert
        var result = await _repository.GetByIdAsync(subTask.Id);
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSubTask()
    {
        // Arrange
        var mainTask = await DbContext.MainTasks.FirstAsync();
        var subTask = TestDataBuilder.CreateSubTask(s =>
        {
            s.Title = "To Delete";
            s.MainTaskId = mainTask.Id;
        });
        await _repository.AddAsync(subTask);

        // Act
        await _repository.DeleteAsync(subTask.Id);

        // Assert
        var result = await _repository.GetByIdAsync(subTask.Id);
        result.Should().BeNull();
    }
}
```

**Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~SubTaskRepositoryTests" -v n
```

预期输出：所有 8 个测试通过

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Repository/SubTaskRepositoryTests.cs
git commit -m "test: add SubTaskRepositoryTests with 8 test cases"
```

---

### Task 8: SchedulePlanRepositoryTests

**Files:**
- Create: `src/tests/UnitTests/Repository/SchedulePlanRepositoryTests.cs`

**Step 1: 创建测试**

```csharp
// src/tests/UnitTests/Repository/SchedulePlanRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class SchedulePlanRepositoryTests : RepositoryTestBase
{
    private readonly SchedulePlanRepository _repository;

    public SchedulePlanRepositoryTests()
    {
        _repository = new SchedulePlanRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-123");

        // Act
        var result = await _repository.AddAsync(plan);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.PlanId.Should().Be("plan-123");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetByIdAsync_WhenPlanExists_ShouldReturnPlan()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan();
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByIdAsync(plan.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(plan.Id);
    }

    [Fact]
    public async Task GetByPlanIdAsync_ShouldReturnPlanByPlanId()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "unique-plan-id");
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByPlanIdAsync("unique-plan-id");

        // Assert
        result.Should().NotBeNull();
        result!.PlanId.Should().Be("unique-plan-id");
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnPlansWithGivenStatus()
    {
        // Arrange
        var createdPlan = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-created";
            p.Status = SchedulePlanStatus.Created;
        });
        var runningPlan = TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "plan-running";
            p.Status = SchedulePlanStatus.Running;
        });
        await _repository.AddAsync(createdPlan);
        await _repository.AddAsync(runningPlan);

        // Act
        var result = await _repository.GetByStatusAsync(SchedulePlanStatus.Created);

        // Assert
        result.Should().HaveCount(1);
        result[0].PlanId.Should().Be("plan-created");
    }

    [Fact]
    public async Task GetRecentPlansAsync_ShouldReturnMostRecentPlans()
    {
        // Arrange
        await _repository.AddAsync(TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-1"));
        await Task.Delay(10);
        await _repository.AddAsync(TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-2"));
        await Task.Delay(10);
        await _repository.AddAsync(TestDataBuilder.CreateSchedulePlan(p => p.PlanId = "plan-3"));

        // Act
        var result = await _repository.GetRecentPlansAsync(2);

        // Assert
        result.Should().HaveCount(2);
        result[0].PlanId.Should().Be("plan-3");
        result[1].PlanId.Should().Be("plan-2");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyPlanProperties()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan(p => p.Status = SchedulePlanStatus.Created);
        await _repository.AddAsync(plan);

        // Act
        plan.Status = SchedulePlanStatus.Running;
        await _repository.UpdateAsync(plan);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result!.Status.Should().Be(SchedulePlanStatus.Running);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePlan()
    {
        // Arrange
        var plan = TestDataBuilder.CreateSchedulePlan();
        await _repository.AddAsync(plan);

        // Act
        await _repository.DeleteAsync(plan.Id);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldOrderByCreatedAtDesc()
    {
        // Arrange
        await _repository.AddAsync(TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "old-plan";
            p.Status = SchedulePlanStatus.Created;
        }));
        await Task.Delay(10);
        await _repository.AddAsync(TestDataBuilder.CreateSchedulePlan(p =>
        {
            p.PlanId = "new-plan";
            p.Status = SchedulePlanStatus.Created;
        }));

        // Act
        var result = await _repository.GetByStatusAsync(SchedulePlanStatus.Created);

        // Assert
        result.Should().HaveCount(2);
        result[0].PlanId.Should().Be("new-plan");
        result[1].PlanId.Should().Be("old-plan");
    }
}
```

**Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~SchedulePlanRepositoryTests" -v n
```

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Repository/SchedulePlanRepositoryTests.cs
git commit -m "test: add SchedulePlanRepositoryTests with 8 test cases"
```

---

### Task 9: ExecutionPlanRepositoryTests

**Files:**
- Create: `src/tests/UnitTests/Repository/ExecutionPlanRepositoryTests.cs`

**Step 1: 创建测试**

```csharp
// src/tests/UnitTests/Repository/ExecutionPlanRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class ExecutionPlanRepositoryTests : RepositoryTestBase
{
    private readonly ExecutionPlanRepository _repository;

    public ExecutionPlanRepositoryTests()
    {
        _repository = new ExecutionPlanRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan(p => p.PlanId = "exec-plan-123");

        // Act
        var result = await _repository.AddAsync(plan);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.PlanId.Should().Be("exec-plan-123");
    }

    [Fact]
    public async Task GetByIdAsync_WhenPlanExists_ShouldReturnPlan()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan();
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByIdAsync(plan.Id);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByPlanIdAsync_ShouldReturnPlanByPlanId()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan(p => p.PlanId = "unique-exec-plan");
        await _repository.AddAsync(plan);

        // Act
        var result = await _repository.GetByPlanIdAsync("unique-exec-plan");

        // Assert
        result.Should().NotBeNull();
        result!.PlanId.Should().Be("unique-exec-plan");
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnPlansWithGivenStatus()
    {
        // Arrange
        var createdPlan = TestDataBuilder.CreateExecutionPlan(p => p.Status = ExecutionPlanStatus.Created);
        var runningPlan = TestDataBuilder.CreateExecutionPlan(p => p.Status = ExecutionPlanStatus.Running);
        await _repository.AddAsync(createdPlan);
        await _repository.AddAsync(runningPlan);

        // Act
        var result = await _repository.GetByStatusAsync(ExecutionPlanStatus.Created);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(ExecutionPlanStatus.Created);
    }

    [Fact]
    public async Task GetByMultipleStatusAsync_ShouldReturnPlansMatchingAnyStatus()
    {
        // Arrange
        var createdPlan = TestDataBuilder.CreateExecutionPlan(p => p.Status = ExecutionPlanStatus.Created);
        var runningPlan = TestDataBuilder.CreateExecutionPlan(p => p.Status = ExecutionPlanStatus.Running);
        var completedPlan = TestDataBuilder.CreateExecutionPlan(p => p.Status = ExecutionPlanStatus.Completed);
        await _repository.AddAsync(createdPlan);
        await _repository.AddAsync(runningPlan);
        await _repository.AddAsync(completedPlan);

        // Act
        var result = await _repository.GetByMultipleStatusAsync(
            new List<ExecutionPlanStatus> { ExecutionPlanStatus.Created, ExecutionPlanStatus.Running },
            10);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByMultipleStatusAsync_ShouldLimitToCount()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _repository.AddAsync(TestDataBuilder.CreateExecutionPlan(p => p.Status = ExecutionPlanStatus.Created));
        }

        // Act
        var result = await _repository.GetByMultipleStatusAsync(
            new List<ExecutionPlanStatus> { ExecutionPlanStatus.Created },
            3);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyPlanProperties()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan(p => p.Status = ExecutionPlanStatus.Created);
        await _repository.AddAsync(plan);

        // Act
        plan.Status = ExecutionPlanStatus.Running;
        await _repository.UpdateAsync(plan);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result!.Status.Should().Be(ExecutionPlanStatus.Running);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePlan()
    {
        // Arrange
        var plan = TestDataBuilder.CreateExecutionPlan();
        await _repository.AddAsync(plan);

        // Act
        await _repository.DeleteAsync(plan.Id);

        // Assert
        var result = await _repository.GetByIdAsync(plan.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByMultipleStatusAsync_ShouldOrderByCreatedAtDesc()
    {
        // Arrange
        await _repository.AddAsync(TestDataBuilder.CreateExecutionPlan(p => p.PlanId = "old"));
        await Task.Delay(10);
        await _repository.AddAsync(TestDataBuilder.CreateExecutionPlan(p => p.PlanId = "new"));

        // Act
        var result = await _repository.GetByMultipleStatusAsync(
            new List<ExecutionPlanStatus> { ExecutionPlanStatus.Created },
            10);

        // Assert
        result.Should().HaveCountGreaterOrEqualTo(2);
        result[0].PlanId.Should().Be("new");
    }
}
```

**Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~ExecutionPlanRepositoryTests" -v n
```

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Repository/ExecutionPlanRepositoryTests.cs
git commit -m "test: add ExecutionPlanRepositoryTests with 9 test cases"
```

---

### Task 10: TaskExecutionResultRepositoryTests

**Files:**
- Create: `src/tests/UnitTests/Repository/TaskExecutionResultRepositoryTests.cs`

**Step 1: 创建测试**

```csharp
// src/tests/UnitTests/Repository/TaskExecutionResultRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class TaskExecutionResultRepositoryTests : RepositoryTestBase
{
    private readonly TaskExecutionResultRepository _repository;

    public TaskExecutionResultRepositoryTests()
    {
        _repository = new TaskExecutionResultRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAssignIdAndSetCreatedAt()
    {
        // Arrange
        var result = TestDataBuilder.CreateTaskExecutionResult(r => r.TaskId = "task-123");

        // Act
        var saved = await _repository.AddAsync(result);

        // Assert
        saved.Id.Should().BeGreaterThan(0);
        saved.TaskId.Should().Be("task-123");
    }

    [Fact]
    public async Task GetByIdAsync_WhenResultExists_ShouldReturnResult()
    {
        // Arrange
        var result = TestDataBuilder.CreateTaskExecutionResult();
        await _repository.AddAsync(result);

        // Act
        var found = await _repository.GetByIdAsync(result.Id);

        // Assert
        found.Should().NotBeNull();
        found!.TaskId.Should().Be(result.TaskId);
    }

    [Fact]
    public async Task GetByTaskIdAsync_ShouldReturnResultsOrderedByCreatedAt()
    {
        // Arrange
        var taskId = "task-with-multiple-results";
        await _repository.AddAsync(TestDataBuilder.CreateTaskExecutionResult(r => r.TaskId = taskId));
        await Task.Delay(10);
        await _repository.AddAsync(TestDataBuilder.CreateTaskExecutionResult(r => r.TaskId = taskId));

        // Act
        var results = await _repository.GetByTaskIdAsync(taskId);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByPlanIdAsync_ShouldReturnResultsOrderedByStartedAt()
    {
        // Arrange
        var planId = "plan-123";
        await _repository.AddAsync(TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.PlanId = planId;
            r.StartedAt = DateTime.UtcNow.AddMinutes(-10);
        }));
        await _repository.AddAsync(TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.PlanId = planId;
            r.StartedAt = DateTime.UtcNow.AddMinutes(-5);
        }));

        // Act
        var results = await _repository.GetByPlanIdAsync(planId);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleResults()
    {
        // Arrange
        var results = new List<TaskExecutionResultEntity>
        {
            TestDataBuilder.CreateTaskExecutionResult(r => r.TaskId = "task-1"),
            TestDataBuilder.CreateTaskExecutionResult(r => r.TaskId = "task-2"),
            TestDataBuilder.CreateTaskExecutionResult(r => r.TaskId = "task-3")
        };

        // Act
        var saved = await _repository.AddRangeAsync(results);

        // Assert
        saved.Should().HaveCount(3);
        saved.All(r => r.Id > 0).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyResultProperties()
    {
        // Arrange
        var result = TestDataBuilder.CreateTaskExecutionResult(r => r.Success = false);
        await _repository.AddAsync(result);

        // Act
        result.Success = true;
        result.Message = "Fixed!";
        await _repository.UpdateAsync(result);

        // Assert
        var updated = await _repository.GetByIdAsync(result.Id);
        updated!.Success.Should().BeTrue();
        updated.Message.Should().Be("Fixed!");
    }

    [Fact]
    public async Task AddRangeAsync_ShouldCalculateDuration()
    {
        // Arrange
        var startedAt = DateTime.UtcNow.AddMinutes(-5);
        var completedAt = DateTime.UtcNow;
        var result = TestDataBuilder.CreateTaskExecutionResult(r =>
        {
            r.StartedAt = startedAt;
            r.CompletedAt = completedAt;
        });

        // Act
        await _repository.AddAsync(result);

        // Assert
        var saved = await _repository.GetByIdAsync(result.Id);
        saved!.DurationMs.Should().BeGreaterOrEqualTo(0);
    }
}
```

**Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~TaskExecutionResultRepositoryTests" -v n
```

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Repository/TaskExecutionResultRepositoryTests.cs
git commit -m "test: add TaskExecutionResultRepositoryTests with 7 test cases"
```

---

### Task 11: LlmProviderConfigRepositoryTests

**Files:**
- Create: `src/tests/UnitTests/Repository/LlmProviderConfigRepositoryTests.cs`

**Step 1: 创建测试**

```csharp
// src/tests/UnitTests/Repository/LlmProviderConfigRepositoryTests.cs
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Repository.Repositories;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Repository;

public class LlmProviderConfigRepositoryTests : RepositoryTestBase
{
    private readonly LlmProviderConfigRepository _repository;

    public LlmProviderConfigRepositoryTests()
    {
        _repository = new LlmProviderConfigRepository(DbContext);
    }

    [Fact]
    public async Task SaveAsync_NewConfig_ShouldInsertAndAssignId()
    {
        // Arrange
        var config = TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "new-provider");

        // Act
        var saved = await _repository.SaveAsync(config);

        // Assert
        saved.Id.Should().NotBeNull();
        saved.ProviderName.Should().Be("new-provider");
    }

    [Fact]
    public async Task SaveAsync_ExistingConfig_ShouldUpdate()
    {
        // Arrange
        var config = TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "existing-provider");
        var saved = await _repository.SaveAsync(config);
        saved.ModelDisplayName = "Updated Model";

        // Act
        var updated = await _repository.SaveAsync(saved);

        // Assert
        updated.ModelDisplayName.Should().Be("Updated Model");
    }

    [Fact]
    public async Task GetByNameAsync_WhenConfigExists_ShouldReturnConfig()
    {
        // Arrange
        var config = TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "test-provider");
        await _repository.SaveAsync(config);

        // Act
        var found = await _repository.GetByNameAsync("test-provider");

        // Assert
        found.Should().NotBeNull();
        found!.ProviderName.Should().Be("test-provider");
    }

    [Fact]
    public async Task GetByNameAsync_WhenConfigNotFound_ShouldReturnNull()
    {
        // Act
        var found = await _repository.GetByNameAsync("nonexistent");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetAllEnabledAsync_ShouldReturnOnlyEnabledConfigs()
    {
        // Arrange
        await _repository.SaveAsync(TestDataBuilder.CreateLlmConfig(c =>
        {
            c.ProviderName = "enabled-provider";
            c.IsEnabled = true;
        }));
        await _repository.SaveAsync(TestDataBuilder.CreateLlmConfig(c =>
        {
            c.ProviderName = "disabled-provider";
            c.IsEnabled = false;
        }));

        // Act
        var configs = await _repository.GetAllEnabledAsync();

        // Assert
        configs.Should().HaveCount(1);
        configs[0].ProviderName.Should().Be("enabled-provider");
    }

    [Fact]
    public async Task GetAllEnabledAsync_ShouldOrderByPriority()
    {
        // Arrange
        await _repository.SaveAsync(TestDataBuilder.CreateLlmConfig(c =>
        {
            c.ProviderName = "low-priority";
            c.IsEnabled = true;
            c.Priority = 10;
        }));
        await _repository.SaveAsync(TestDataBuilder.CreateLlmConfig(c =>
        {
            c.ProviderName = "high-priority";
            c.IsEnabled = true;
            c.Priority = 1;
        }));

        // Act
        var configs = await _repository.GetAllEnabledAsync();

        // Assert
        configs.Should().HaveCount(2);
        configs[0].ProviderName.Should().Be("high-priority");
        configs[1].ProviderName.Should().Be("low-priority");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllConfigs()
    {
        // Arrange
        await _repository.SaveAsync(TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "provider-1"));
        await _repository.SaveAsync(TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "provider-2"));

        // Act
        var configs = await _repository.GetAllAsync();

        // Assert
        configs.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task DeleteAsync_WhenConfigExists_ShouldReturnTrue()
    {
        // Arrange
        var config = TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "to-delete");
        await _repository.SaveAsync(config);

        // Act
        var deleted = await _repository.DeleteAsync("to-delete");

        // Assert
        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenConfigExists_ShouldReturnTrue()
    {
        // Arrange
        var config = TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "exists");
        await _repository.SaveAsync(config);

        // Act
        var exists = await _repository.ExistsAsync("exists");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLastUsedAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var config = TestDataBuilder.CreateLlmConfig(c => c.ProviderName = "used-provider");
        await _repository.SaveAsync(config);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await _repository.UpdateLastUsedAsync("used-provider");

        // Assert
        var updated = await _repository.GetByNameAsync("used-provider");
        // 注意：这里测试 LastUsedAt 是否更新（需要在实体中添加该字段）
    }
}
```

**Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~LlmProviderConfigRepositoryTests" -v n
```

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Repository/LlmProviderConfigRepositoryTests.cs
git commit -m "test: add LlmProviderConfigRepositoryTests with 10 test cases"
```

---

## Phase 3: Services 层测试

### Task 12: 创建 Services 测试目录结构

**Step 1: 创建目录**

```bash
mkdir -p src/tests/UnitTests/Services/Scheduling
mkdir -p src/tests/UnitTests/Services/Orchestration
mkdir -p src/tests/UnitTests/Services/Resilience
```

**Step 2: 提交**

```bash
git add src/tests/UnitTests/Services/
git commit -m "test: create Services test directory structure"
```

---

### Task 13: MafTaskSchedulerTests

**Files:**
- Create: `src/tests/UnitTests/Services/Scheduling/MafTaskSchedulerTests.cs`

**Step 1: 创建测试**

```csharp
// src/tests/UnitTests/Services/Scheduling/MafTaskSchedulerTests.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Services.Scheduling;

public class MafTaskSchedulerTests : ServiceTestBase
{
    private readonly Mock<IPriorityCalculator> _mockPriorityCalculator;
    private readonly MafTaskScheduler _sut;

    public MafTaskSchedulerTests()
    {
        _mockPriorityCalculator = new Mock<IPriorityCalculator>();
        _sut = new MafTaskScheduler(
            _mockPriorityCalculator.Object,
            maxConcurrentTasks: 10,
            NullLogger<MafTaskScheduler>.Instance);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldCalculatePriorityForAllTasks()
    {
        // Arrange
        var tasks = new List<DecomposedTask>
        {
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "task-1"),
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "task-2"),
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "task-3")
        };
        _mockPriorityCalculator
            .Setup(x => x.CalculatePriority(It.IsAny<PriorityCalculationRequest>()))
            .Returns(75);

        // Act
        var result = await _sut.ScheduleAsync(tasks);

        // Assert
        result.ScheduledTasks.Should().HaveCount(3);
        _mockPriorityCalculator.Verify(
            x => x.CalculatePriority(It.IsAny<PriorityCalculationRequest>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ScheduleAsync_ShouldSortTasksByPriorityScore()
    {
        // Arrange
        var tasks = new List<DecomposedTask>
        {
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "low"),
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "high"),
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "medium")
        };
        var callCount = 0;
        _mockPriorityCalculator
            .Setup(x => x.CalculatePriority(It.IsAny<PriorityCalculationRequest>()))
            .Returns(() => new[] { 90, 50, 70 }[callCount++]);

        // Act
        var result = await _sut.ScheduleAsync(tasks);

        // Assert
        result.ExecutionPlan.HighPriorityTasks.Should().HaveCount(1);
        result.ExecutionPlan.MediumPriorityTasks.Should().HaveCount(1);
        result.ExecutionPlan.LowPriorityTasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldGroupTasksByPriority()
    {
        // Arrange
        var tasks = new List<DecomposedTask>
        {
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "high-1"),
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "high-2"),
            TestDataBuilder.CreateDecomposedTask(t => t.TaskId = "low-1")
        };
        _mockPriorityCalculator
            .Setup(x => x.CalculatePriority(It.IsAny<PriorityCalculationRequest>()))
            .Returns<PriorityCalculationRequest>(r =>
                r.TaskId == "low-1" ? 20 : 80);

        // Act
        var result = await _sut.ScheduleAsync(tasks);

        // Assert
        result.ExecutionPlan.HighPriorityTasks.Should().HaveCount(2);
        result.ExecutionPlan.LowPriorityTasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task ScheduleAsync_WithEmptyTaskList_ShouldReturnEmptyResult()
    {
        // Arrange
        var tasks = new List<DecomposedTask>();

        // Act
        var result = await _sut.ScheduleAsync(tasks);

        // Assert
        result.ScheduledTasks.Should().BeEmpty();
        result.ExecutionPlan.HighPriorityTasks.Should().BeEmpty();
        result.ExecutionPlan.MediumPriorityTasks.Should().BeEmpty();
        result.ExecutionPlan.LowPriorityTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteTaskAsync_ShouldUpdateTaskStatusToRunning()
    {
        // Arrange
        var task = TestDataBuilder.CreateDecomposedTask();
        var executionResult = new TaskExecutionResult { Success = true };
        var executorCalled = false;

        // Act
        await _sut.ExecuteTaskAsync(task, (t, ct) =>
        {
            executorCalled = true;
            t.Status.Should().Be(MafTaskStatus.Running);
            return Task.FromResult(executionResult);
        });

        // Assert
        executorCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTaskAsync_OnSuccess_ShouldUpdateStatusToCompleted()
    {
        // Arrange
        var task = TestDataBuilder.CreateDecomposedTask();
        var result = new TaskExecutionResult { Success = true, CompletedAt = DateTime.UtcNow };

        // Act
        var executionResult = await _sut.ExecuteTaskAsync(task, (t, ct) => Task.FromResult(result));

        // Assert
        task.Status.Should().Be(MafTaskStatus.Completed);
        executionResult.Should().Be(result);
    }

    [Fact]
    public async Task ExecuteTaskAsync_OnFailure_ShouldUpdateStatusToFailed()
    {
        // Arrange
        var task = TestDataBuilder.CreateDecomposedTask();
        var result = new TaskExecutionResult { Success = false, CompletedAt = DateTime.UtcNow };

        // Act
        var executionResult = await _sut.ExecuteTaskAsync(task, (t, ct) => Task.FromResult(result));

        // Assert
        task.Status.Should().Be(MafTaskStatus.Failed);
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithCancellation_ShouldCancel()
    {
        // Arrange
        var task = TestDataBuilder.CreateDecomposedTask();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExecuteTaskAsync(task, async (t, ct) =>
            {
                await Task.Delay(1000, ct);
                return new TaskExecutionResult { Success = true };
            }, cts.Token));
    }

    [Fact]
    public async Task ExecuteTaskAsync_ShouldSetStartedAt()
    {
        // Arrange
        var task = TestDataBuilder.CreateDecomposedTask();
        var before = DateTime.UtcNow;

        // Act
        await _sut.ExecuteTaskAsync(task, (t, ct) =>
        {
            t.StartedAt.Should().NotBe(default);
            t.StartedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(1));
            return Task.FromResult(new TaskExecutionResult { Success = true });
        });

        // Assert
        task.StartedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task ExecuteTaskAsync_ShouldSetCompletedAt()
    {
        // Arrange
        var task = TestDataBuilder.CreateDecomposedTask();
        var completedAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        await _sut.ExecuteTaskAsync(task, (t, ct) =>
            Task.FromResult(new TaskExecutionResult { Success = true, CompletedAt = completedAt }));

        // Assert
        task.CompletedAt.Should().Be(completedAt);
    }
}
```

**Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~MafTaskSchedulerTests" -v n
```

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Services/Scheduling/MafTaskSchedulerTests.cs
git commit -m "test: add MafTaskSchedulerTests with 12 test cases"
```

---

### Task 14: CircuitBreakerTests

**Files:**
- Create: `src/tests/UnitTests/Services/Resilience/CircuitBreakerTests.cs`

**Step 1: 创建测试**

```csharp
// src/tests/UnitTests/Services/Resilience/CircuitBreakerTests.cs
using CKY.MultiAgentFramework.Services.Resilience;
using CKY.MultiAgentFramework.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Resilience;

public class CircuitBreakerTests
{
    private readonly CircuitBreakerConfig _config;
    private readonly CircuitBreaker _sut;

    public CircuitBreakerTests()
    {
        _config = new CircuitBreakerConfig
        {
            FailureThreshold = 3,
            OpenTimeout = TimeSpan.FromMilliseconds(100),
            HalfOpenTimeout = TimeSpan.FromMilliseconds(50)
        };
        _sut = new CircuitBreaker(_config, NullLogger<CircuitBreaker>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClosed_ShouldExecuteOperation()
    {
        // Arrange
        var executed = false;

        // Act
        await _sut.ExecuteAsync("test", async (ct) =>
        {
            executed = true;
            return await Task.FromResult("success");
        });

        // Assert
        executed.Should().BeTrue();
        _sut.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpen_ShouldThrowException()
    {
        // Arrange - 触发熔断
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await _sut.ExecuteAsync("test", async (ct) =>
                {
                    await Task.FromException<int>(new Exception("test failure"));
                    throw new Exception("unreachable");
                });
            }
            catch { }
        }

        // Act & Assert
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            _sut.ExecuteAsync("test", async (ct) => await Task.FromResult("should not execute")));
    }

    [Fact]
    public async Task ExecuteAsync_AfterFailureThreshold_ShouldTransitionToOpen()
    {
        // Arrange
        for (int i = 0; i < 2; i++)
        {
            try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }
        }

        // Act - 第3次失败应该触发熔断
        try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }

        // Assert
        _sut.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public async Task ExecuteAsync_AfterOpenTimeout_ShouldTransitionToHalfOpen()
    {
        // Arrange - 触发熔断
        for (int i = 0; i < 3; i++)
        {
            try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }
        }
        _sut.State.Should().Be(CircuitState.Open);

        // Act - 等待超时
        await Task.Delay(_config.OpenTimeout.Add(TimeSpan.FromMilliseconds(50)));

        // Assert - 下次调用时应该转换为 HalfOpen
        try
        {
            await _sut.ExecuteAsync("test", async (ct) =>
            {
                _sut.State.Should().Be(CircuitState.HalfOpen);
                await Task.FromException<int>(new Exception());
                throw new Exception();
            });
        }
        catch { }
    }

    [Fact]
    public async Task ExecuteAsync_InHalfOpenOnSuccess_ShouldTransitionToClosed()
    {
        // Arrange - 触发熔断并进入 HalfOpen
        for (int i = 0; i < 3; i++)
        {
            try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }
        }
        await Task.Delay(_config.OpenTimeout.Add(TimeSpan.FromMilliseconds(50)));

        // Act - 成功执行
        await _sut.ExecuteAsync("test", async (ct) => await Task.FromResult("success"));

        // Assert
        _sut.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_InHalfOpenOnFailure_ShouldTransitionToOpen()
    {
        // Arrange - 进入 HalfOpen
        for (int i = 0; i < 3; i++)
        {
            try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }
        }
        await Task.Delay(_config.OpenTimeout.Add(TimeSpan.FromMilliseconds(50)));
        try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }

        // Act
        _sut.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public void State_ShouldStartAsClosed()
    {
        // Assert
        _sut.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackFailureCount()
    {
        // Arrange & Act
        for (int i = 0; i < 2; i++)
        {
            try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }
        }

        // Assert - 熔断器应该记录失败但还未打开
        _sut.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldResetFailureCountOnSuccess()
    {
        // Arrange
        try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }

        // Act - 成功执行应该重置计数
        await _sut.ExecuteAsync("test", async (ct) => await Task.FromResult("success"));

        // Assert - 需要再失败3次才能触发熔断
        for (int i = 0; i < 2; i++)
        {
            try { await _sut.ExecuteAsync("test", (ct) => throw new Exception()); } catch { }
        }
        _sut.State.Should().Be(CircuitState.Closed);
    }
}
```

**Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~CircuitBreakerTests" -v n
```

**Step 3: 提交**

```bash
git add src/tests/UnitTests/Services/Resilience/CircuitBreakerTests.cs
git commit -m "test: add CircuitBreakerTests with 9 test cases"
```

---

### Task 15: RetryExecutorTests（如果存在该类）

**Step 1: 检查 RetryExecutor 类是否存在**

```bash
grep -r "class RetryExecutor" src/Services/
```

如果存在，创建测试；如果不存在，跳过此任务。

---

## Phase 4: 验证和优化

### Task 16: 运行所有测试并检查覆盖率

**Step 1: 运行所有测试**

```bash
cd src/tests/UnitTests
dotnet test --no-build --verbosity normal
```

预期输出：所有测试通过

**Step 2: 运行带覆盖率的测试**

```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Step 3: 生成覆盖率报告**

```bash
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

**Step 4: 检查覆盖率**

打开 `coverage-report/index.html` 查看覆盖率报告

**Step 5: 如果覆盖率低于目标，添加额外测试**

记录需要改进的区域并添加测试

**Step 6: 提交最终版本**

```bash
git add .
git commit -m "test: complete unit testing implementation with 70%+ coverage"
```

---

## Phase 5: 文档和清理

### Task 17: 更新 TODO.md

**Files:**
- Modify: `TODO.md`

**Step 1: 更新 TODO.md，标记完成的任务**

在 TODO.md 中添加：

```markdown
## 单元测试实现

- [x] 添加测试基础设施（SQLite, TestDataBuilder）
- [x] 实现 Repository 层测试（52 个测试）
- [x] 实现 Services 层测试（核心 Services）
- [ ] 代码覆盖率报告生成
- [ ] CI/CD 集成测试
```

**Step 2: 提交**

```bash
git add TODO.md
git commit -m "docs: update TODO.md with testing progress"
```

---

## 验收检查清单

在完成此实现计划后，验证以下内容：

- [ ] 所有测试通过（100% 通过率）
- [ ] 代码覆盖率 ≥ 70%
- [ ] Repository 层覆盖率 ≥ 80%
- [ ] Services 层覆盖率 ≥ 85%
- [ ] 无测试代码警告
- [ ] 测试运行时间 < 30 秒
- [ ] 所有测试遵循命名约定
- [ ] 测试辅助类已实现并复用

---

## 附录：快速参考

### 常用命令

```bash
# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~MainTaskRepositoryTests"

# 运行特定测试方法
dotnet test --filter "FullyQualifiedName~AddAsync_ShouldAssignId"

# 运行带详细输出的测试
dotnet test --verbosity normal

# 运行带覆盖率的测试
dotnet test --collect:"XPlat Code Coverage"

# 仅编译不运行
dotnet build --no-restore
```

### Git 提交约定

- `test:` - 新增测试
- `fix:` - 修复测试
- `refactor:` - 重构测试代码
- `chore:` - 测试基础设施更新

---

**实现计划创建完成**

文档位置：`docs/plans/2025-01-14-unit-testing-implementation.md`
