using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CKY.MultiAgentFramework.Infrastructure.Dapper;

/// <summary>
/// 数据库初始化服务
/// 在应用启动时自动创建必要的表结构
/// </summary>
internal sealed class DatabaseInitializer
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDbConnection connection, ILogger<DatabaseInitializer> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize()
    {
        try
        {
            _logger.LogInformation("Initializing database tables...");

            // 检查表是否存在
            var tableExists = CheckIfTableExists("MainTasks");

            if (!tableExists)
            {
                _logger.LogInformation("Tables not found. Creating table structures...");

                CreateMainTasksTable();
                CreateSubTasksTable();
                CreateMafAiSessionsTable();
                CreateChatMessagesTable();
                CreateLlmProviderConfigsTable();
                CreateSchemaVersionTable();

                _logger.LogInformation("Database tables created successfully.");

                // 将成功信息写入文件
                try
                {
                    var successLogPath = Path.Combine(Path.GetTempPath(), "maf_db_init_success.log");
                    File.WriteAllText(successLogPath, $"[{DateTime.Now}] Database tables created successfully at: {DateTime.Now}");
                }
                catch
                {
                    // 忽略日志写入错误
                }
            }
            else
            {
                _logger.LogInformation("Database tables already exist. Skipping initialization.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database: {Message}", ex.Message);

            // 将错误信息写入文件
            try
            {
                var errorLogPath = Path.Combine(Path.GetTempPath(), "maf_db_init_error.log");
                var errorInfo = $"[{DateTime.Now}] Database initialization failed:\n" +
                                $"Error: {ex.Message}\n" +
                                $"StackTrace: {ex.StackTrace}\n";
                File.WriteAllText(errorLogPath, errorInfo);
            }
            catch
            {
                // 忽略日志写入错误
            }

            throw;
        }
    }

    private bool CheckIfTableExists(string tableName)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@TableName";

            var param = command.CreateParameter();
            param.ParameterName = "@TableName";
            param.Value = tableName;
            command.Parameters.Add(param);

            var result = command.ExecuteScalar();
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    private void CreateMainTasksTable()
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS MainTasks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Description TEXT,
    Priority TEXT NOT NULL DEFAULT 'Normal',
    Status TEXT NOT NULL DEFAULT 'Pending',
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    CONSTRAINT chk_main_tasks_priority CHECK (Priority IN ('Low', 'Normal', 'High', 'Critical')),
    CONSTRAINT chk_main_tasks_status CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled'))
);";

        ExecuteSql(sql, "MainTasks");
    }

    private void CreateSubTasksTable()
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS SubTasks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MainTaskId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT,
    Status TEXT NOT NULL DEFAULT 'Pending',
    ExecutionOrder INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT fk_sub_tasks_main_task FOREIGN KEY (MainTaskId)
        REFERENCES MainTasks(Id) ON DELETE CASCADE,
    CONSTRAINT chk_sub_tasks_status CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled')
);";

        ExecuteSql(sql, "SubTasks");
    }

    private void CreateMafAiSessionsTable()
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS MafAiSessions (
    SessionId TEXT NOT NULL PRIMARY KEY,
    UserId TEXT,
    Metadata TEXT,
    Items TEXT,
    Status TEXT NOT NULL DEFAULT 'Active',
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    LastActivityAt TEXT NOT NULL DEFAULT (datetime('now')),
    ExpiresAt TEXT,
    TotalTokensUsed INTEGER NOT NULL DEFAULT 0,
    TurnCount INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT chk_sessions_status CHECK (Status IN ('Active', 'Suspended', 'Completed', 'Cancelled', 'Expired', 'Error'))
);";

        ExecuteSql(sql, "MafAiSessions");
    }

    private void CreateChatMessagesTable()
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS ChatMessages (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId TEXT NOT NULL,
    Role TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT fk_chat_messages_session FOREIGN KEY (SessionId)
        REFERENCES MafAiSessions(SessionId) ON DELETE CASCADE,
    CONSTRAINT chk_chat_messages_role CHECK (Role IN ('User', 'Assistant', 'System'))
);";

        ExecuteSql(sql, "ChatMessages");
    }

    private void CreateLlmProviderConfigsTable()
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS LlmProviderConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderName TEXT NOT NULL,
    ProviderDisplayName TEXT NOT NULL DEFAULT '',
    ApiBaseUrl TEXT NOT NULL DEFAULT '',
    ApiKey TEXT NOT NULL DEFAULT '',
    ModelId TEXT NOT NULL DEFAULT '',
    ModelDisplayName TEXT NOT NULL DEFAULT '',
    SupportedScenariosJson TEXT NOT NULL DEFAULT '[]',
    MaxTokens INTEGER NOT NULL DEFAULT 2000,
    Temperature REAL NOT NULL DEFAULT 0.7,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    Priority INTEGER NOT NULL DEFAULT 0,
    CostPer1kTokens REAL NOT NULL DEFAULT 0,
    AdditionalParametersJson TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    LastUsedAt TEXT,
    Notes TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_llm_provider_name ON LlmProviderConfigs(ProviderName);
CREATE INDEX IF NOT EXISTS idx_llm_provider_enabled ON LlmProviderConfigs(IsEnabled);
CREATE INDEX IF NOT EXISTS idx_llm_provider_priority ON LlmProviderConfigs(Priority);";

        ExecuteSql(sql, "LlmProviderConfigs");
    }

    private void CreateSchemaVersionTable()
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS SchemaVersion (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScriptName TEXT NOT NULL UNIQUE,
    AppliedAt TEXT NOT NULL DEFAULT (datetime('now')),
    Checksum TEXT
);";

        ExecuteSql(sql, "SchemaVersion");
    }

    private void ExecuteSql(string sql, string tableName)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
            _logger.LogInformation("Created table: {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create table {TableName}: {Message}", tableName, ex.Message);
            throw;
        }
    }
}
