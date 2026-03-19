# ============================================================================
# CKY.MAF 框架数据库初始化指南
# ============================================================================
# 版本: 1.0.0
# 更新时间: 2025-03-16
# ============================================================================

## 快速开始

### 1. 自动初始化（推荐）

```bash
# Linux/Mac
cd scripts/database
bash migrate-apply-maf-framework.sh

# Windows PowerShell
cd scripts\database
powershell .\migrate-apply-maf-framework.ps1
```

### 2. 手动初始化

```bash
# SQLite
cd scripts/database
sqlite3 ../../../maf.db < maf-framework/001_create_maf_framework_tables.sql

# PostgreSQL
psql -U maf -d mafdb -f maf-framework/001_create_maf_framework_tables.sql
```

## 数据库表说明

### 1. **任务管理表**

#### MainTasks（主任务表）
- 用途：存储主任务信息
- 字段：Id, Title, Description, Priority, Status, CreatedAt, UpdatedAt
- 索引：Status, Priority, CreatedAt

#### SubTasks（子任务表）
- 用途：存储子任务信息
- 字段：Id, MainTaskId, Title, Description, Status, ExecutionOrder
- 外键：MainTaskId → MainTasks(Id)
- 索引：MainTaskId, Status, ExecutionOrder

### 2. **会话管理表**

#### MafAiSessions（Agent 会话表）
- 用途：存储 AI Agent 会话状态
- 字段：SessionId, UserId, Metadata, Items, Status, CreatedAt, LastActivityAt, ExpiresAt, TotalTokensUsed, TurnCount
- 主键：SessionId
- 索引：UserId, Status, CreatedAt, LastActivityAt, ExpiresAt

#### ChatMessages（聊天消息表）
- 用途：存储聊天消息历史
- 字段：Id, SessionId, Role, Content, CreatedAt
- 外键：SessionId → MafAiSessions(SessionId)
- 索引：SessionId, CreatedAt, Role

### 3. **版本管理表**

#### SchemaVersion（数据库版本表）
- 用途：跟踪迁移脚本执行历史
- 字段：Id, ScriptName, AppliedAt, Checksum

## 数据类型映射

### C# → SQLite

| C# 类型 | SQLite 类型 | 说明 |
|---------|------------|------|
| `int` | `INTEGER` | 主键、外键 |
| `string` | `TEXT` | 字符串 |
| `DateTime` | `TEXT` | ISO 8601 格式 |
| `bool` | `INTEGER` | 0/1 |
| `long` | `INTEGER` | 大整数 |
| `enum` | `TEXT` | 枚举名称 |
| `Dictionary` | `TEXT` | JSON 序列化 |

### C# → PostgreSQL

| C# 类型 | PostgreSQL 类型 | 说明 |
|---------|----------------|------|
| `int` | `SERIAL` | 自增主键 |
| `string` | `TEXT` / `VARCHAR` | 字符串 |
| `DateTime` | `TIMESTAMP` | 时间戳 |
| `bool` | `BOOLEAN` | 布尔值 |
| `long` | `BIGINT` | 大整数 |
| `enum` | `TEXT` / `VARCHAR` | 枚举名称 |
| `Dictionary` | `JSONB` | JSON 对象 |

## 约束说明

### 枚举值约束

#### TaskPriority（任务优先级）
- `Low` - 低优先级
- `Normal` - 普通优先级（默认）
- `High` - 高优先级
- `Critical` - 关键优先级

#### MafTaskStatus（任务状态）
- `Pending` - 待处理（默认）
- `Running` - 运行中
- `Completed` - 已完成
- `Failed` - 已失败
- `Cancelled` - 已取消

#### SessionStatus（会话状态）
- `Active` - 活跃中（默认）
- `Suspended` - 已暂停
- `Completed` - 已完成
- `Cancelled` - 已取消
- `Expired` - 已过期
- `Error` - 错误状态

#### ChatRole（聊天角色）
- `User` - 用户
- `Assistant` - 助手
- `System` - 系统

## 查询示例

### 查询待处理的主任务
```sql
SELECT * FROM MainTasks WHERE Status = 'Pending' ORDER BY Priority DESC, CreatedAt ASC;
```

### 查询活跃会话
```sql
SELECT * FROM MafAiSessions
WHERE Status = 'Active'
  AND (ExpiresAt IS NULL OR ExpiresAt > datetime('now'))
ORDER BY LastActivityAt DESC;
```

### 查询最近的聊天消息
```sql
SELECT * FROM ChatMessages
WHERE SessionId = 'session-id-123'
ORDER BY CreatedAt DESC
LIMIT 50;
```

### 统计 Token 使用量
```sql
SELECT
    UserId,
    COUNT(*) as SessionCount,
    SUM(TotalTokensUsed) as TotalTokens,
    SUM(TurnCount) as TotalTurns
FROM MafAiSessions
WHERE Status = 'Completed'
GROUP BY UserId;
```

## 性能优化建议

### 1. 索引优化
```sql
-- 复合索引（针对常见查询）
CREATE INDEX idx_main_tasks_status_priority ON MainTasks(Status, Priority);
CREATE INDEX idx_chat_messages_session_created ON ChatMessages(SessionId, CreatedAt DESC);
```

### 2. 定期清理
```sql
-- 删除过期会话（超过 30 天）
DELETE FROM MafAiSessions
WHERE ExpiresAt IS NOT NULL
  AND ExpiresAt < datetime('now', '-30 days');

-- 删除旧聊天消息（超过 90 天）
DELETE FROM ChatMessages
WHERE CreatedAt < datetime('now', '-90 days');
```

### 3. 数据库分析
```bash
# SQLite 数据库大小
ls -lh maf.db

# 表大小统计
sqlite3 maf.db "SELECT name, (sum(pgsize) / 1024.0) AS size_kb FROM dbstat GROUP BY name;"

# 索引分析
sqlite3 maf_db "PRAGMA index_list('MainTasks');"
```

## 故障排查

### 问题1：数据库锁定
```bash
# 检查锁定进程
lsof | grep maf.db

# 强制解锁（谨慎使用）
rm -f maf.db-wal maf.db-shm
```

### 问题2：表已存在
```bash
# 删除所有表并重新创建
sqlite3 maf.db <<EOF
DROP TABLE IF EXISTS ChatMessages;
DROP TABLE IF EXISTS MafAiSessions;
DROP TABLE IF EXISTS SubTasks;
DROP TABLE IF EXISTS MainTasks;
DROP TABLE IF EXISTS SchemaVersion;
EOF

# 重新运行初始化脚本
bash migrate-apply-maf-framework.sh
```

### 问题3：权限错误（PostgreSQL）
```bash
# 授予用户权限
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE mafdb TO maf;"
psql -U postgres -d mafdb -c "GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO maf;"
psql -U postgres -d mafdb -c "GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO maf;"
```

## 验证安装

### 运行验证查询
```bash
sqlite3 maf.db <<EOF
SELECT 'MainTasks' as Table, COUNT(*) as Count FROM MainTasks
UNION ALL
SELECT 'SubTasks', COUNT(*) FROM SubTasks
UNION ALL
SELECT 'MafAiSessions', COUNT(*) FROM MafAiSessions
UNION ALL
SELECT 'ChatMessages', COUNT(*) FROM ChatMessages
UNION ALL
SELECT 'SchemaVersion', COUNT(*) FROM SchemaVersion;
EOF
```

### 预期输出
```
Table          Count
--------------  -----
MainTasks      1
SubTasks       1
MafAiSessions  0
ChatMessages   0
SchemaVersion   0
```

## 更新日志

### v1.0.0 (2025-03-16)
- ✅ 初始版本
- ✅ 创建 5 个核心表
- ✅ 添加所有必要的索引和约束
- ✅ 包含示例数据
- ✅ 支持 SQLite 和 PostgreSQL

---

## 联系方式

如有问题或建议，请联系开发团队或提交 Issue。
