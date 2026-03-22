using System.Text.Json;
using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;

/// <summary>
/// 持久化用户行为服务 - EF Core 实现
/// </summary>
public class PersistentUserBehaviorService : IUserBehaviorService
{
    private readonly CustomerServiceDbContext _db;
    private readonly ILogger<PersistentUserBehaviorService> _logger;

    public PersistentUserBehaviorService(CustomerServiceDbContext db, ILogger<PersistentUserBehaviorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordAsync(UserBehaviorRecord record, CancellationToken ct = default)
    {
        var entity = new UserBehaviorRecordEntity
        {
            UserId = record.UserId,
            SessionId = record.SessionId,
            Intent = record.Intent,
            TaskSucceeded = record.TaskSucceeded,
            ClarificationRoundsNeeded = record.ClarificationRoundsNeeded,
            ResponseTimeMs = (long)record.ResponseTime.TotalMilliseconds,
            EntitiesJson = record.Entities.Count > 0 ? JsonSerializer.Serialize(record.Entities) : null,
            Timestamp = record.Timestamp
        };

        _db.UserBehaviorRecords.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default)
    {
        var records = await _db.UserBehaviorRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        if (records.Count == 0) return null;

        var profile = new UserProfile
        {
            UserId = userId,
            TotalInteractions = records.Count,
            LastActiveTime = records.First().Timestamp
        };

        // 统计意图频率
        foreach (var record in records)
        {
            profile.IntentFrequency.TryGetValue(record.Intent, out var count);
            profile.IntentFrequency[record.Intent] = count + 1;
        }

        // 从最近的记录中提取默认实体
        foreach (var record in records.Take(20))
        {
            if (string.IsNullOrEmpty(record.EntitiesJson)) continue;
            var entities = DeserializeEntities(record.EntitiesJson);
            foreach (var kv in entities)
            {
                // 最近使用的实体优先
                profile.DefaultEntities.TryAdd(kv.Key, kv.Value);
            }
        }

        // 统计常用分类（基于意图前缀）
        profile.FrequentCategories = profile.IntentFrequency
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => kv.Key)
            .ToList();

        return profile;
    }

    public async Task<Dictionary<string, string>> GetDefaultEntitiesAsync(string userId, CancellationToken ct = default)
    {
        var profile = await GetUserProfileAsync(userId, ct);
        return profile?.DefaultEntities ?? new Dictionary<string, string>();
    }

    private static Dictionary<string, string> DeserializeEntities(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
