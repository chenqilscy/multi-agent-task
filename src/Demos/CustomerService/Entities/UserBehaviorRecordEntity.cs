namespace CKY.MultiAgentFramework.Demos.CustomerService.Entities;

/// <summary>
/// 用户行为记录实体
/// </summary>
public class UserBehaviorRecordEntity
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string Intent { get; set; } = string.Empty;

    public bool TaskSucceeded { get; set; }

    public int ClarificationRoundsNeeded { get; set; }

    /// <summary>响应时间（毫秒）</summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>提取的实体 JSON</summary>
    public string? EntitiesJson { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
