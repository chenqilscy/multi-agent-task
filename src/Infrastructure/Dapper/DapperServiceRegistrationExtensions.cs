using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using System.Collections;
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Infrastructure.Dapper;

/// <summary>
/// Dapper 服务注册扩展方法
/// </summary>
public static class DapperServiceRegistrationExtensions
{
    /// <summary>
    /// 注册 Dapper 关系数据库实现（MAF 框架默认）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddDapperRelationalDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["MafStorage:RelationalDatabase:Provider"] ?? "SQLite";

        if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            var dbPath = configuration["MafStorage:RelationalDatabase:SqlitePath"] ?? "maf.db";

            services.AddSingleton<IDbConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DatabaseInitializer>>();
                var env = sp.GetRequiredService<IHostEnvironment>();

                // 使用应用程序基础目录
                var appBaseDirectory = AppContext.BaseDirectory;
                var fullPath = Path.IsPathRooted(dbPath)
                    ? dbPath
                    : Path.Combine(appBaseDirectory, dbPath);

                logger.LogInformation("Using SQLite database at: {DbPath}", fullPath);
                logger.LogInformation("App Base Directory: {AppBaseDirectory}", appBaseDirectory);
                logger.LogInformation("Content Root Path: {ContentRootPath}", env.ContentRootPath);

                var connectionString = $"Data Source={fullPath}";
                var connection = new SqliteConnection(connectionString);
                connection.Open();

                // 创建数据库初始化服务
                var dbInitializer = new DatabaseInitializer(connection, logger);
                dbInitializer.Initialize();

                return connection;
            });
        }
        else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string is required when Provider is set to PostgreSQL");
            }

            services.AddSingleton<IDbConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DapperRelationalDatabase>>();
                logger.LogInformation("Using PostgreSQL database");

                var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return connection;
            });
        }
        else
        {
            throw new NotSupportedException(
                $"Database provider '{provider}' is not supported. " +
                "Supported providers: SQLite, PostgreSQL");
        }

        services.AddScoped<IRelationalDatabase, DapperRelationalDatabase>();

        return services;
    }
}
