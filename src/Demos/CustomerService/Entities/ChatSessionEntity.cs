namespace CKY.MultiAgentFramework.Demos.CustomerService.Entities;

/// <summary>
/// 对话会话实体
/// </summary>
public class ChatSessionEntity
{
    public int Id { get; set; }

    /// <summary>会话ID（GUID）</summary>
    public string SessionId { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    /// <summary>active/closed</summary>
    public string Status { get; set; } = "active";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    /// <summary>对话摘要（关闭时生成）</summary>
    public string? Summary { get; set; }

    // 导航属性
    public CustomerEntity? Customer { get; set; }
    public List<ChatMessageEntity> Messages { get; set; } = new();
}

/// <summary>
/// 对话消息实体
/// </summary>
public class ChatMessageEntity
{
    public int Id { get; set; }

    public int ChatSessionEntityId { get; set; }

    /// <summary>user/assistant/system</summary>
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>识别的意图</summary>
    public string? Intent { get; set; }

    /// <summary>提取的实体 JSON</summary>
    public string? EntitiesJson { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // 导航属性
    public ChatSessionEntity ChatSession { get; set; } = null!;
}
