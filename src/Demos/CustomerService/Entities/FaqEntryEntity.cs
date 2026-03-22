namespace CKY.MultiAgentFramework.Demos.CustomerService.Entities;

/// <summary>
/// FAQ 知识库条目实体
/// </summary>
public class FaqEntryEntity
{
    public int Id { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// <summary>关键词 JSON 数组</summary>
    public string KeywordsJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
