# Dapper 关系数据库使用指南

本文档介绍如何在 CKY.MAF 中使用 `IRelationalDatabase`（Dapper 实现），涵盖 SQLite 与 PostgreSQL 两种模式的配置、切换和最佳实践。

## 目录

- [快速开始](#快速开始)
- [SQLite 配置（开发/Demo）](#sqlite-配置开发demo)
- [PostgreSQL 配置（生产环境）](#postgresql-配置生产环境)
- [切换数据库提供商](#切换数据库提供商)
- [表结构](#表结构)
- [Repository 使用示例](#repository-使用示例)
- [直接 SQL 使用](#直接-sql-使用)
- [注意事项](#注意事项)

---

## 快速开始

### 1. 注册服务

在 `Program.cs` 中添加：

```csharp
builder.Services.AddDapperRelationalDatabase(builder.Configuration);
```

### 2. 注入使用

```csharp
public class MyService
{
    private readonly IRelationalDatabase _db;

    public MyService(IRelationalDatabase db)
    {
        _db = db;
    }

    public async Task<List<T>> QueryAsync<T>(string sql, object? param = null)
    {
        return await _db.ExecuteSqlAsync<T>(sql, param);
    }
}
```

---

## SQLite 配置（开发/Demo）

SQLite 是默认数据库，**零配置即可使用**。数据库文件自动创建在应用程序运行目录下。

### 最小配置

```json
{
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "SQLite"
    }
  }
}
```

### 自定义数据库文件路径

```json
{
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "SQLite",
      "SqlitePath": "data/my-app.db"
    }
  }
}
```

- 相对路径基于 `AppContext.BaseDirectory`
- 支持绝对路径：`"SqlitePath": "C:\\data\\maf.db"`

### 特点

| 项目 | 说明 |
|------|------|
| 配置要求 | 零配置，开箱即用 |
| 外部依赖 | 无，SQLite 以文件形式嵌入 |
| 适用场景 | 本地开发、单元测试、Demo |
| 并发支持 | 有限（单写多读） |
| 数据持久化 | 文件级别，不支持分布式 |

---

## PostgreSQL 配置（生产环境）

PostgreSQL 提供企业级事务支持和高并发能力，推荐用于生产环境。

### 配置步骤

**1. 修改 appsettings.json**：

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=mafdb;Username=maf;Password=your-password"
  },
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  }
}
```

> **安全提示**：生产环境中不要在配置文件中硬编码密码，使用环境变量或 User Secrets。

**2. 使用环境变量（推荐）**：

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=${PG_HOST};Port=5432;Database=mafdb;Username=${PG_USER};Password=${PG_PASSWORD}"
  }
}
```

或通过 .NET User Secrets：

```bash
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=mafdb;Username=maf;Password=secret"
```

**3. Docker 快速启动 PostgreSQL**：

```bash
docker run -d \
  --name maf-postgres \
  -e POSTGRES_USER=maf \
  -e POSTGRES_PASSWORD=your-password \
  -e POSTGRES_DB=mafdb \
  -p 5432:5432 \
  postgres:16
```

或使用项目 `docker-compose.yml`：

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: maf
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: mafdb
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

### 特点

| 项目 | 说明 |
|------|------|
| 配置要求 | 需要连接字符串 + PostgreSQL 服务 |
| 外部依赖 | PostgreSQL 服务器（Docker/托管/云） |
| 适用场景 | 生产环境、高并发、多实例部署 |
| 并发支持 | 完善的 MVCC 事务隔离 |
| 数据持久化 | 服务端存储、支持备份和复制 |

---

## 切换数据库提供商

只需修改配置中的 `Provider` 字段即可切换，**代码无需任何改动**。

### 从 SQLite 切换到 PostgreSQL

1. 准备 PostgreSQL 服务
2. 修改配置：

```diff
  "MafStorage": {
    "RelationalDatabase": {
-     "Provider": "SQLite"
+     "Provider": "PostgreSQL"
    }
  }
```

3. 添加连接字符串：

```diff
+ "ConnectionStrings": {
+   "PostgreSQL": "Host=localhost;Database=mafdb;Username=maf;Password=***"
+ },
```

4. 重启应用，框架自动创建表结构

### 按环境切换

利用 .NET 配置优先级机制：

- `appsettings.json` → SQLite（默认）
- `appsettings.Production.json` → PostgreSQL（生产覆盖）

```json
// appsettings.json
{
  "MafStorage": {
    "RelationalDatabase": { "Provider": "SQLite" }
  }
}

// appsettings.Production.json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=pg-server;Database=mafdb;Username=maf;Password=***"
  },
  "MafStorage": {
    "RelationalDatabase": { "Provider": "PostgreSQL" }
  }
}
```

---

## 表结构

框架启动时自动创建以下 5 张表（SQLite 和 PostgreSQL 均已支持自动初始化）：

| 表名 | 用途 | 主键类型 |
|------|------|---------|
| MainTasks | 主任务 | 自增 INTEGER / SERIAL |
| SubTasks | 子任务 | 自增 INTEGER / SERIAL |
| MafAiSessions | AI 对话会话 | TEXT (UUID) |
| ChatMessages | 对话消息 | 自增 INTEGER / SERIAL |
| SchemaVersion | 数据库版本追踪 | 自增 INTEGER / SERIAL |

### SQLite 与 PostgreSQL DDL 差异

| 特性 | SQLite | PostgreSQL |
|------|--------|-----------|
| 自增主键 | `INTEGER PRIMARY KEY AUTOINCREMENT` | `SERIAL PRIMARY KEY` |
| 时间类型 | `TEXT` + `datetime('now')` | `TIMESTAMP` + `NOW()` |
| 表存在检查 | `sqlite_master` | `information_schema.tables` |
| 大小写敏感 | 不敏感 | 敏感（使用引号包裹列名） |

---

## Repository 使用示例

### 通过 IRelationalDatabase 接口

```csharp
public class TaskService
{
    private readonly IRelationalDatabase _db;

    public TaskService(IRelationalDatabase db) => _db = db;

    // 插入
    public async Task<MainTask> CreateAsync(MainTask task)
    {
        return await _db.InsertAsync(task);
    }

    // 查询列表
    public async Task<IEnumerable<MainTask>> GetPendingAsync()
    {
        return await _db.GetListAsync<MainTask>("Status = @Status",
            new { Status = "Pending" });
    }

    // 按 ID 查询
    public async Task<MainTask?> GetByIdAsync(int id)
    {
        return await _db.GetByIdAsync<MainTask>(id);
    }

    // 更新
    public async Task UpdateAsync(MainTask task)
    {
        await _db.UpdateAsync(task);
    }

    // 删除
    public async Task DeleteAsync(int id)
    {
        await _db.DeleteAsync<MainTask>(id);
    }
}
```

### 自定义 SQL 查询

```csharp
// 执行自定义 SQL
var results = await _db.ExecuteSqlAsync<ChatHistoryMessage>(
    "SELECT Role, Content, CreatedAt FROM ChatMessages WHERE SessionId = @SessionId ORDER BY Id ASC LIMIT @Limit",
    new { SessionId = "abc-123", Limit = 50 });

// 事务操作
await _db.ExecuteInTransactionAsync(async () =>
{
    var task = new MainTask { Title = "新任务" };
    await _db.InsertAsync(task);
    await _db.InsertAsync(new SubTask { MainTaskId = task.Id, Title = "子任务1" });
});
```

---

## 注意事项

### SQL 兼容性

由于 SQLite 和 PostgreSQL 在语法上有差异，编写自定义 SQL 时请注意：

| 操作 | SQLite | PostgreSQL | 兼容写法 |
|------|--------|-----------|---------|
| 字符串连接 | `\|\|` | `\|\|` | 相同 |
| 时间戳 | `datetime('now')` | `NOW()` | 用参数传入 |
| 分页 | `LIMIT x OFFSET y` | `LIMIT x OFFSET y` | 相同 |
| UPSERT | `INSERT OR REPLACE` | `ON CONFLICT ... DO UPDATE` | 分别处理 |
| RETURNING | `RETURNING *`（3.35+） | `RETURNING *` | 相同 |
| 布尔值 | 0/1 | true/false | 用 0/1 |

**推荐做法**：尽量使用 `IRelationalDatabase` 的 CRUD 方法（`InsertAsync`、`UpdateAsync` 等），这些方法由 Dapper 自动生成兼容的 SQL。
