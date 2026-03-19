namespace CKY.MultiAgentFramework.Infrastructure.Dapper;

/// <summary>
/// 用于指定实体对应的数据库表名
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute : Attribute
{
    /// <summary>
    /// 数据库表名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 数据库 Schema（可选，仅适用于 PostgreSQL 等支持 Schema 的数据库）
    /// </summary>
    public string? Schema { get; set; }

    public TableAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
