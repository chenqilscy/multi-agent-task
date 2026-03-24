using System.Data;
using System.Text.Json;
using Dapper;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Dapper;

/// <summary>
/// 基于 Dapper 的 LLM 提供商配置仓储实现
/// </summary>
public sealed class DapperLlmProviderConfigRepository : ILlmProviderConfigRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DapperLlmProviderConfigRepository> _logger;

    public DapperLlmProviderConfigRepository(
        IDbConnection connection,
        ILogger<DapperLlmProviderConfigRepository> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LlmProviderConfig?> GetByNameAsync(
        string providerName,
        CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM LlmProviderConfigs WHERE ProviderName = @ProviderName";

        var entity = await _connection.QueryFirstOrDefaultAsync<LlmProviderConfigEntity>(
            new CommandDefinition(sql, new { ProviderName = providerName }, cancellationToken: ct));

        return entity?.ToDomainModel();
    }

    public async Task<List<LlmProviderConfig>> GetAllEnabledAsync(
        CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM LlmProviderConfigs WHERE IsEnabled = 1 ORDER BY Priority";

        var entities = await _connection.QueryAsync<LlmProviderConfigEntity>(
            new CommandDefinition(sql, cancellationToken: ct));

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    public async Task<List<LlmProviderConfig>> GetAllAsync(
        CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM LlmProviderConfigs ORDER BY Priority";

        var entities = await _connection.QueryAsync<LlmProviderConfigEntity>(
            new CommandDefinition(sql, cancellationToken: ct));

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    public async Task<List<LlmProviderConfig>> GetByScenarioAsync(
        LlmScenario scenario,
        CancellationToken ct = default)
    {
        // 内存筛选：加载所有启用的配置后按场景过滤
        var allEnabled = await GetAllEnabledAsync(ct);
        return allEnabled
            .Where(c => c.SupportedScenarios.Contains(scenario))
            .OrderBy(c => c.Priority)
            .ToList();
    }

    public async Task<LlmProviderConfig> SaveAsync(
        LlmProviderConfig config,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validate();

        var existing = await _connection.QueryFirstOrDefaultAsync<LlmProviderConfigEntity>(
            new CommandDefinition(
                "SELECT * FROM LlmProviderConfigs WHERE ProviderName = @ProviderName",
                new { config.ProviderName },
                cancellationToken: ct));

        if (existing != null)
        {
            existing.UpdateFromDomainModel(config);
            const string updateSql = @"
UPDATE LlmProviderConfigs SET
    ProviderDisplayName = @ProviderDisplayName,
    ApiBaseUrl = @ApiBaseUrl,
    ApiKey = @ApiKey,
    ModelId = @ModelId,
    ModelDisplayName = @ModelDisplayName,
    SupportedScenariosJson = @SupportedScenariosJson,
    MaxTokens = @MaxTokens,
    Temperature = @Temperature,
    IsEnabled = @IsEnabled,
    Priority = @Priority,
    CostPer1kTokens = @CostPer1kTokens,
    AdditionalParametersJson = @AdditionalParametersJson,
    UpdatedAt = @UpdatedAt
WHERE ProviderName = @ProviderName";

            await _connection.ExecuteAsync(
                new CommandDefinition(updateSql, existing, cancellationToken: ct));
            return existing.ToDomainModel();
        }
        else
        {
            var entity = LlmProviderConfigEntity.FromDomainModel(config);
            const string insertSql = @"
INSERT INTO LlmProviderConfigs
    (ProviderName, ProviderDisplayName, ApiBaseUrl, ApiKey, ModelId, ModelDisplayName,
     SupportedScenariosJson, MaxTokens, Temperature, IsEnabled, Priority,
     CostPer1kTokens, AdditionalParametersJson, CreatedAt)
VALUES
    (@ProviderName, @ProviderDisplayName, @ApiBaseUrl, @ApiKey, @ModelId, @ModelDisplayName,
     @SupportedScenariosJson, @MaxTokens, @Temperature, @IsEnabled, @Priority,
     @CostPer1kTokens, @AdditionalParametersJson, @CreatedAt)
RETURNING *";

            var inserted = await _connection.QueryFirstOrDefaultAsync<LlmProviderConfigEntity>(
                new CommandDefinition(insertSql, entity, cancellationToken: ct));
            return (inserted ?? entity).ToDomainModel();
        }
    }

    public async Task<bool> DeleteAsync(
        string providerName,
        CancellationToken ct = default)
    {
        const string sql = "DELETE FROM LlmProviderConfigs WHERE ProviderName = @ProviderName";

        var affected = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { ProviderName = providerName }, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> ExistsAsync(
        string providerName,
        CancellationToken ct = default)
    {
        const string sql =
            "SELECT COUNT(1) FROM LlmProviderConfigs WHERE ProviderName = @ProviderName";

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { ProviderName = providerName }, cancellationToken: ct));
        return count > 0;
    }

    public async Task UpdateLastUsedAsync(
        string providerName,
        CancellationToken ct = default)
    {
        const string sql =
            "UPDATE LlmProviderConfigs SET LastUsedAt = @Now WHERE ProviderName = @ProviderName";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { ProviderName = providerName, Now = DateTime.UtcNow.ToString("o") },
                cancellationToken: ct));
    }
}
