# ============================================================================
# CKY.MAF 数据库连接字符串配置指南
# ============================================================================
# 版本: 1.0.0
# 更新时间: 2025-03-16
# ============================================================================

## 架构概述

### 数据库使用情况

```
┌─────────────────────────────────────────────────────────────┐
│                    应用架构                                 │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  CustomerService Demo (使用 Dapper)                         │
│  ├── AddMafDapperServices()                                 │
│  └── 连接字符串: "MafFramework"                             │
│                                                               │
│  SmartHome Demo (使用 EF Core)                              │
│  ├── AddMafBuiltinServices()                                 │
│  └── 连接字符串: "DefaultConnection" (MafDbContext)          │
│                                                               │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    数据库层                                   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  开发环境: SQLite                                             │
│  ├── maf.db (共享数据库)                                      │
│  │   ├── MafAiSessions (框架表)                              │
│  │   ├── MainTasks (框架表)                                   │
│  │   ├── SubTasks (框架表)                                    │
│  │   ├── ChatMessages (框架表)                                │
│  │   └── biz_Customers (业务表 - 前缀区分)                   │
│  │   └── biz_Orders (业务表)                                  │
│                                                               │
│  生产环境: PostgreSQL                                         │
│  ├── maf_framework (框架数据库)                               │
│  │   └── 框架表...                                             │
│  └── customer_service (业务数据库)                            │
│      └── 业务表...                                             │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 配置方案

### **方案1：共享数据库（推荐用于开发）**

#### **配置说明**
- MAF 框架和 CustomerService 使用同一个数据库
- 业务表使用前缀（如 `biz_`）区分
- 配置简单，易于管理

#### **appsettings.json**
```json
{
  "ConnectionStrings": {
    "MafFramework": "Data Source=maf.db"
  },
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "SQLite",
      "SqlitePath": "maf.db"
    }
  }
}
```

#### **使用方式**
```csharp
// Program.cs - CustomerService
builder.Services.AddMafDapperServices(builder.Configuration);

// IRelationalDatabase 会自动使用 "MafFramework" 连接字符串
```

---

### **方案2：独立数据库（推荐用于生产）**

#### **配置说明**
- MAF 框架使用独立数据库
- CustomerService 使用独立数据库
- 支持独立扩展和备份

#### **appsettings.Production.json**
```json
{
  "ConnectionStrings": {
    "MafFramework": "Host=postgres-server;Port=5432;Database=maf_framework;Username=maf;Password=***",
    "CustomerService": "Host=postgres-server;Port=5432;Database=customer_service;Username=maf;Password=***"
  },
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  }
}
```

#### **业务层自定义 DbContext（可选）**
```csharp
// Program.cs - CustomerService
builder.Services.AddMafDapperServices(builder.Configuration); // MAF 框架

// 如果 CustomerService 需要独立的数据库连接
builder.Services.AddDbContext<CustomerServiceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CustomerService")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
```

---

## 详细配置示例

### **1. SQLite 开发环境**

#### **appsettings.Development.json**
```json
{
  "ConnectionStrings": {
    "MafFramework": "Data Source=maf.db",
    "Redis": "localhost:6379"
  },
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "SQLite",
      "SqlitePath": "maf.db"
    }
  }
}
```

#### **Dapper 连接配置**
```csharp
// 自动使用 "MafFramework" 连接字符串
services.AddDapperRelationalDatabase(configuration);
```

---

### **2. PostgreSQL 生产环境**

#### **appsettings.Production.json**
```json
{
  "ConnectionStrings": {
    "MafFramework": "Host=postgres-production.example.com;Port=5432;Database=maf_framework;Username=maf_user;Password=secure_password;SSL Mode=Require;Maximum Pool Size=100;Connection Lifetime=300;",
    "Redis": "redis-production.example.com:6379,password=redis_password,ssl=true"
  },
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  }
}
```

#### **连接池优化**
```csharp
// DapperServiceRegistrationExtensions.cs 已包含连接池支持
services.AddSingleton<IDbConnection>(sp =>
{
    var connectionString = configuration.GetConnectionString("MafFramework");
    var connection = new NpgsqlConnection(connectionString);
    connection.Open();
    return connection;
});
```

---

## SmartHome 配置（EF Core）

### **appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=smarthome.db",
    "PostgreSQL": "Host=localhost;Port=5432;Database=smarthome;Username=smarthome;Password=***"
  }
}
```

### **Program.cs - SmartHome**
```csharp
// 使用 EF Core
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 或者注册自定义 DbContext
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite("Data Source=smarthome.db"));
```

---

## 连接字符串参考

### **SQLite**

```
Data Source=filename.db
Data Source=C:\path\to\filename.db
Data Source=|DataDirectory|filename.db
```

### **PostgreSQL**

```
Host=myserver;Port=5432;Database=mydb;Username=myuser;Password=mypassword
Host=myserver;Port=5432;Database=mydb;Username=myuser;Password=mypassword;SSL Mode=Require
Host=myserver;Port=5432;Database=mydb;Username=myuser;Password=mypassword;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100
```

### **Redis**

```
localhost:6379
localhost:6379,password=mypassword
redis-server:6379,password=redis_password,ssl=true
```

---

## 配置优先级

1. **appsettings.json** - 基础配置
2. **appsettings.{Environment}.json** - 环境特定配置
3. **环境变量** - 最高优先级

### **环境变量示例**

```bash
# Linux/Mac
export ConnectionStrings__MafFramework="Data Source=maf.db"
export MafStorage__RelationalDatabase__Provider="SQLite"

# Windows PowerShell
$env:ConnectionStrings__MafFramework="Data Source=maf.db"
$env:MafStorage__RelationalDatabase__Provider="SQLite"

# Windows CMD
set ConnectionStrings__MafFramework=Data Source=maf.db
set MafStorage__RelationalDatabase__Provider=SQLite
```

---

## 验证配置

### **1. 检查连接字符串**

```csharp
// Program.cs
var app = builder.Build();

// 输出配置信息
var mafConnectionString = builder.Configuration.GetConnectionString("MafFramework");
app.Logger.LogInformation("MAF Framework Connection: {ConnectionString}", mafConnectionString);

var provider = builder.Configuration["MafStorage:RelationalDatabase:Provider"];
app.Logger.LogInformation("Database Provider: {Provider}", provider);

app.Run();
```

### **2. 测试数据库连接**

```bash
# SQLite
sqlite3 maf.db ".tables"

# PostgreSQL
psql -U maf -d maf_framework -c "\dt"
```

---

## 故障排查

### **问题1：连接字符串未找到**

```
错误: Unable to resolve service for type 'IDbConnection'
```

**解决方案：**
1. 检查 `appsettings.json` 中是否配置了 `ConnectionStrings:MafFramework`
2. 检查 `MafStorage:RelationalDatabase:Provider` 是否正确

### **问题2：数据库文件权限错误**

```
错误: Unable to open database file
```

**解决方案：**
```bash
# SQLite
chmod 666 maf.db

# PostgreSQL
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE maf_framework TO maf_user;"
```

### **问题3：PostgreSQL 连接失败**

```
错误: FATAL: password authentication failed
```

**解决方案：**
1. 检查用户名和密码是否正确
2. 检查 `pg_hba.conf` 配置
3. 确保 PostgreSQL 服务正在运行

---

## 最佳实践

### **1. 使用环境变量**
```bash
# 开发环境
export ASPNETCORE_ENVIRONMENT=Development
export CONNECTION_STRINGS__MAF_FRAMEWORK="Data Source=maf.db"

# 生产环境
export ASPNETCORE_ENVIRONMENT=Production
export CONNECTION_STRINGS__MAF_FRAMEWORK="Host=postgres-server;Port=5432;Database=maf_framework;..."
```

### **2. 使用 User Secrets（开发环境）**
```bash
cd src/Demos/CustomerService
dotnet user-secrets set "ConnectionStrings:MafFramework" "Data Source=maf.db"
```

### **3. 连接字符串加密（生产环境）**
```bash
# 加密连接字符串
dotnet user-secrets set "ConnectionStrings:MafFramework" "Encrypted:..."

# 或使用 Azure Key Vault / HashiCorp Vault
```

---

## 总结

| 场景 | 连接字符串键 | 数据库 | 说明 |
|------|-------------|--------|------|
| **CustomerService 开发** | `MafFramework` | SQLite (maf.db) | 共享数据库 |
| **CustomerService 生产** | `MafFramework` | PostgreSQL | 共享数据库 |
| **SmartHome 开发** | `DefaultConnection` | SQLite (smarthome.db) | EF Core DbContext |
| **SmartHome 生产** | `PostgreSQL` | PostgreSQL | EF Core DbContext |

**配置文件位置：**
- [appsettings.json](src/Demos/CustomerService/appsettings.json) - 基础配置
- [appsettings.Development.json](src/Demos/CustomerService/appsettings.Development.json) - 开发环境
- [appsettings.Production.json](src/Demos/CustomerService/appsettings.Production.json) - 生产环境

**详细文档：** [README.md](scripts/database/README.md)
