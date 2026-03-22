using Microsoft.Extensions.Logging;
using System.Data;

namespace CKY.MultiAgentFramework.Infrastructure.Dapper;

/// <summary>
/// PostgreSQL 数据库初始化服务
/// 在应用启动时自动创建必要的表结构（PostgreSQL 语法）
/// </summary>
internal sealed class PostgreSqlDatabaseInitializer
{
    private readonly IDbConnection _connection;
    private readonly ILogger _logger;

    public PostgreSqlDatabaseInitializer(IDbConnection connection, ILogger logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize()
    {
        try
        {
            _logger.LogInformation("Initializing PostgreSQL database tables...");

            var tableExists = CheckIfTableExists("maintasks");

            if (!tableExists)
            {
                _logger.LogInformation("Tables not found. Creating table structures...");

                CreateMainTasksTable();
                CreateSubTasksTable();
                CreateMafAiSessionsTable();
                CreateChatMessagesTable();
                CreateSchemaVersionTable();

                _logger.LogInformation("PostgreSQL database tables created successfully.");
            }
            else
            {
                _logger.LogInformation("Database tables already exist. Skipping initialization.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PostgreSQL database: {Message}", ex.Message);
            throw;
        }
    }

    private bool CheckIfTableExists(string tableName)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText =
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND LOWER(table_name) = @TableName";

            var param = command.CreateParameter();
            param.ParameterName = "@TableName";
            param.Value = tableName.ToLowerInvariant();
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
        const string sql = """
            CREATE TABLE IF NOT EXISTS "MainTasks" (
                "Id" SERIAL PRIMARY KEY,
                "Title" TEXT NOT NULL,
                "Description" TEXT,
                "Priority" TEXT NOT NULL DEFAULT 'Normal',
                "Status" TEXT NOT NULL DEFAULT 'Pending',
                "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
                "UpdatedAt" TIMESTAMP,
                CONSTRAINT chk_main_tasks_priority CHECK ("Priority" IN ('Low', 'Normal', 'High', 'Critical')),
                CONSTRAINT chk_main_tasks_status CHECK ("Status" IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled'))
            );
            """;

        ExecuteSql(sql, "MainTasks");
    }

    private void CreateSubTasksTable()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "SubTasks" (
                "Id" SERIAL PRIMARY KEY,
                "MainTaskId" INTEGER NOT NULL,
                "Title" TEXT NOT NULL,
                "Description" TEXT,
                "Status" TEXT NOT NULL DEFAULT 'Pending',
                "ExecutionOrder" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT fk_sub_tasks_main_task FOREIGN KEY ("MainTaskId")
                    REFERENCES "MainTasks"("Id") ON DELETE CASCADE,
                CONSTRAINT chk_sub_tasks_status CHECK ("Status" IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled'))
            );
            """;

        ExecuteSql(sql, "SubTasks");
    }

    private void CreateMafAiSessionsTable()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "MafAiSessions" (
                "SessionId" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT,
                "Metadata" TEXT,
                "Items" TEXT,
                "Status" TEXT NOT NULL DEFAULT 'Active',
                "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
                "LastActivityAt" TIMESTAMP NOT NULL DEFAULT NOW(),
                "ExpiresAt" TIMESTAMP,
                "TotalTokensUsed" INTEGER NOT NULL DEFAULT 0,
                "TurnCount" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT chk_sessions_status CHECK ("Status" IN ('Active', 'Suspended', 'Completed', 'Cancelled', 'Expired', 'Error'))
            );
            """;

        ExecuteSql(sql, "MafAiSessions");
    }

    private void CreateChatMessagesTable()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "ChatMessages" (
                "Id" SERIAL PRIMARY KEY,
                "SessionId" TEXT NOT NULL,
                "Role" TEXT NOT NULL,
                "Content" TEXT NOT NULL,
                "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
                CONSTRAINT fk_chat_messages_session FOREIGN KEY ("SessionId")
                    REFERENCES "MafAiSessions"("SessionId") ON DELETE CASCADE,
                CONSTRAINT chk_chat_messages_role CHECK ("Role" IN ('User', 'Assistant', 'System'))
            );
            """;

        ExecuteSql(sql, "ChatMessages");
    }

    private void CreateSchemaVersionTable()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "SchemaVersion" (
                "Id" SERIAL PRIMARY KEY,
                "ScriptName" TEXT NOT NULL UNIQUE,
                "AppliedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
                "Checksum" TEXT
            );
            """;

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
