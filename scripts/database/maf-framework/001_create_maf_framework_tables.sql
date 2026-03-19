-- ============================================================================
-- CKY.MAF 框架数据库表结构
-- ============================================================================
-- 版本: 1.0.0
-- 创建时间: 2025-03-16
-- 描述: MAF 框架所有实体的数据库表结构
--
-- 支持数据库:
--   - SQLite (默认，开发环境)
--   - PostgreSQL (生产环境)
--
-- 使用方式:
--   SQLite:  sqlite3 maf.db < 001_create_maf_framework_tables.sql
--   PostgreSQL: psql -U maf -d mafdb -f 001_create_maf_framework_tables.sql
--
-- 注意事项:
--   1. SQLite 使用 INTEGER PRIMARY KEY AUTOINCREMENT
--   2. PostgreSQL 使用 SERIAL PRIMARY KEY
--   3. 枚举类型存储为 TEXT，需要在应用层转换
--   4. JSON 字段在 SQLite 中存储为 TEXT
--   5. 时间字段统一使用 ISO 8601 格式字符串
-- ============================================================================

-- ============================================================================
-- 1. 任务管理表 (Task Management)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1.1 主任务表 (MainTasks)
-- ----------------------------------------------------------------------------
-- 描述: 存储主任务信息，包含任务的基本属性、状态和优先级
-- 关联: SubTasks (一对多)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS MainTasks (
    -- 主键
    Id INTEGER PRIMARY KEY AUTOINCREMENT,  -- SQLite
    -- Id SERIAL PRIMARY KEY,              -- PostgreSQL

    -- 基本信息
    Title TEXT NOT NULL,                   -- 任务标题
    Description TEXT,                       -- 任务描述（可选）

    -- 任务属性
    Priority TEXT NOT NULL DEFAULT 'Normal',  -- 任务优先级: Low, Normal, High, Critical
    Status TEXT NOT NULL DEFAULT 'Pending',    -- 任务状态: Pending, Running, Completed, Failed, Cancelled

    -- 时间戳
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),  -- ISO 8601 格式
    -- CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,   -- PostgreSQL
    UpdatedAt TEXT,                                       -- 最后更新时间

    -- 索引
    CONSTRAINT chk_main_tasks_priority CHECK (Priority IN ('Low', 'Normal', 'High', 'Critical')),
    CONSTRAINT chk_main_tasks_status CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled'))
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_main_tasks_status ON MainTasks(Status);
CREATE INDEX IF NOT EXISTS idx_main_tasks_priority ON MainTasks(Priority);
CREATE INDEX IF NOT EXISTS idx_main_tasks_created_at ON MainTasks(CreatedAt);

-- ----------------------------------------------------------------------------
-- 1.2 子任务表 (SubTasks)
-- ----------------------------------------------------------------------------
-- 描述: 存储子任务信息，每个子任务属于一个主任务
-- 关联: MainTasks (多对一)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS SubTasks (
    -- 主键
    Id INTEGER PRIMARY KEY AUTOINCREMENT,  -- SQLite
    -- Id SERIAL PRIMARY KEY,              -- PostgreSQL

    -- 外键
    MainTaskId INTEGER NOT NULL,             -- 主任务ID

    -- 基本信息
    Title TEXT NOT NULL,                    -- 子任务标题
    Description TEXT,                        -- 子任务描述（可选）

    -- 任务属性
    Status TEXT NOT NULL DEFAULT 'Pending',  -- 子任务状态
    ExecutionOrder INTEGER NOT NULL DEFAULT 0, -- 执行顺序

    -- 外键约束
    CONSTRAINT fk_sub_tasks_main_task FOREIGN KEY (MainTaskId)
        REFERENCES MainTasks(Id) ON DELETE CASCADE,

    -- 约束
    CONSTRAINT chk_sub_tasks_status CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled'))
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_sub_tasks_main_task_id ON SubTasks(MainTaskId);
CREATE INDEX IF NOT EXISTS idx_sub_tasks_status ON SubTasks(Status);
CREATE INDEX IF NOT EXISTS idx_sub_tasks_execution_order ON SubTasks(ExecutionOrder);

-- ============================================================================
-- 2. 会话管理表 (Session Management)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 2.1 Agent 会话表 (MafAiSessions)
-- ----------------------------------------------------------------------------
-- 描述: 存储 MAF AI Agent 的会话状态和上下文信息
-- 用途: 多轮对话、会话恢复、Token 统计
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS MafAiSessions (
    -- 主键（使用 SessionId 作为主键，更符合业务逻辑）
    SessionId TEXT NOT NULL PRIMARY KEY,

    -- 用户信息
    UserId TEXT,                           -- 用户标识符（可选）

    -- 会话数据（JSON 序列化）
    Metadata TEXT,                         -- 会话元数据（JSON）
    Items TEXT,                            -- 会话数据项（JSON）

    -- 会话状态
    Status TEXT NOT NULL DEFAULT 'Active',  -- 会话状态: Active, Suspended, Completed, Cancelled, Expired, Error

    -- 时间戳
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    LastActivityAt TEXT NOT NULL DEFAULT (datetime('now')),
    ExpiresAt TEXT,                        -- 过期时间（可选）

    -- 统计信息
    TotalTokensUsed INTEGER NOT NULL DEFAULT 0,  -- 累计 Token 使用量
    TurnCount INTEGER NOT NULL DEFAULT 0,        -- 对话轮次计数

    -- 约束
    CONSTRAINT chk_sessions_status CHECK (Status IN ('Active', 'Suspended', 'Completed', 'Cancelled', 'Expired', 'Error'))
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_maf_sessions_user_id ON MafAiSessions(UserId);
CREATE INDEX IF NOT EXISTS idx_maf_sessions_status ON MafAiSessions(Status);
CREATE INDEX IF NOT EXISTS idx_maf_sessions_created_at ON MafAiSessions(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_maf_sessions_last_activity_at ON MafAiSessions(LastActivityAt);
CREATE INDEX IF NOT EXISTS idx_maf_sessions_expires_at ON MafAiSessions(ExpiresAt);

-- ============================================================================
-- 3. 聊天历史表 (Chat History)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 3.1 聊天消息表 (ChatMessages)
-- ----------------------------------------------------------------------------
-- 描述: 存储 Agent 聊天消息历史，支持多轮对话
-- 关联: MafAiSessions (多对一)
-- 用途: L3 持久化存储，配合 L2 缓存使用
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ChatMessages (
    -- 主键
    Id INTEGER PRIMARY KEY AUTOINCREMENT,  -- SQLite
    -- Id SERIAL PRIMARY KEY,              -- PostgreSQL

    -- 关联会话
    SessionId TEXT NOT NULL,              -- 会话ID

    -- 消息内容
    Role TEXT NOT NULL,                   -- 角色: User, Assistant, System
    Content TEXT NOT NULL,                -- 消息内容

    -- 时间戳
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),

    -- 外键约束
    CONSTRAINT fk_chat_messages_session FOREIGN KEY (SessionId)
        REFERENCES MafAiSessions(SessionId) ON DELETE CASCADE,

    -- 约束
    CONSTRAINT chk_chat_messages_role CHECK (Role IN ('User', 'Assistant', 'System'))
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_chat_messages_session_id ON ChatMessages(SessionId);
CREATE INDEX IF NOT EXISTS idx_chat_messages_created_at ON ChatMessages(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_chat_messages_role ON ChatMessages(Role);

-- ============================================================================
-- 4. 数据版本管理表 (Schema Version)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 4.1 数据库版本表 (SchemaVersion)
-- ----------------------------------------------------------------------------
-- 描述: 跟踪数据库迁移脚本的执行历史
-- 用途: 支持回滚和版本管理
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS SchemaVersion (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScriptName TEXT NOT NULL UNIQUE,     -- 脚本名称
    AppliedAt TEXT NOT NULL DEFAULT (datetime('now')),  -- 应用时间
    Checksum TEXT                        -- 脚本校验和（可选）
);

-- ============================================================================
-- 5. 示例数据（可选）
-- ============================================================================

-- 插入示例主任务
INSERT INTO MainTasks (Title, Description, Priority, Status)
VALUES
    ('测试主任务', '这是一个测试主任务', 'Normal', 'Pending');

-- 插入示例子任务
INSERT INTO SubTasks (MainTaskId, Title, Description, Status, ExecutionOrder)
SELECT
    Id,
    '测试子任务1',
    '这是主任务的子任务',
    'Pending',
    1
FROM MainTasks
WHERE Title = '测试主任务';

-- ============================================================================
-- 6. PostgreSQL 特定优化（可选）
-- ============================================================================
/*
-- 如果使用 PostgreSQL，可以添加以下优化：

-- 1. 启用自动更新时间戳
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_main_tasks_updated_at
    BEFORE UPDATE ON MainTasks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- 2. 添加全文搜索支持
CREATE INDEX idx_main_tasks_title_gin ON MainTasks USING gin(to_tsvector('english', Title));
CREATE INDEX idx_chat_messages_content_gin ON ChatMessages USING gin(to_tsvector('english', Content));

-- 3. 添加 JSONB 字段支持
-- ALTER TABLE MafAiSessions ALTER COLUMN Metadata TYPE JSONB USING Metadata::jsonb;
-- ALTER TABLE MafAiSessions ALTER COLUMN Items TYPE JSONB USING Items::jsonb;

-- 4. 创建视图
CREATE VIEW v_ActiveSessions AS
SELECT * FROM MafAiSessions
WHERE Status = 'Active'
  AND (ExpiresAt IS NULL OR ExpiresAt > CURRENT_TIMESTAMP);

-- 5. 创建分区表（大数据量优化）
-- CREATE TABLE ChatMessages_2025 PARTITION OF ChatMessages
--     FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');
*/

-- ============================================================================
-- 7. 维护命令
-- ============================================================================

/*
-- 查看表结构
.schema MainTasks

-- 查看所有表
.tables

-- 查看索引
.indexes

-- 查看外键
PRAGMA foreign_keys;

-- 分析查询性能
EXPLAIN QUERY PLAN SELECT * FROM MainTasks WHERE Status = 'Pending';

-- 清空表（保留结构）
DELETE FROM MainTasks;

-- 删除表
DROP TABLE IF EXISTS ChatMessages;
DROP TABLE IF EXISTS MafAiSessions;
DROP TABLE IF EXISTS SubTasks;
DROP TABLE IF EXISTS MainTasks;
DROP TABLE IF EXISTS SchemaVersion;
*/

-- ============================================================================
-- 结束
-- ============================================================================
-- 脚本执行完成！
--
-- 验证安装:
--   SELECT COUNT(*) FROM MainTasks;
--   SELECT COUNT(*) FROM SubTasks;
--   SELECT COUNT(*) FROM MafAiSessions;
--   SELECT COUNT(*) FROM ChatMessages;
--
-- ============================================================================
