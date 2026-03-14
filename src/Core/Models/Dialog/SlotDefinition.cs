namespace CKY.MultiAgentFramework.Core.Models.Dialog;

/// <summary>
/// 定义槽位的数据类型
/// Defines the data type of a slot
/// </summary>
public enum SlotType
{
    /// <summary>
    /// 字符串类型
    /// String type
    /// </summary>
    String,

    /// <summary>
    /// 整数类型
    /// Integer type
    /// </summary>
    Integer,

    /// <summary>
    /// 浮点数类型
    /// Floating-point number type
    /// </summary>
    Float,

    /// <summary>
    /// 布尔类型
    /// Boolean type
    /// </summary>
    Boolean,

    /// <summary>
    /// 日期时间类型
    /// Date and time type
    /// </summary>
    DateTime,

    /// <summary>
    /// 枚举类型（值必须在预定义列表中）
    /// Enumeration type (values must be in predefined list)
    /// </summary>
    Enumeration,

    /// <summary>
    /// 实体类型（如设备名称、地点等）
    /// Entity type (such as device name, location, etc.)
    /// </summary>
    Entity,

    /// <summary>
    /// 对象类型（复杂的嵌套结构）
    /// Object type (complex nested structure)
    /// </summary>
    Object,

    /// <summary>
    /// 数组类型（值的集合）
    /// Array type (collection of values)
    /// </summary>
    Array
}

/// <summary>
/// 槽位定义，描述意图中需要填充的参数
/// Slot definition, describing parameters to be filled in an intent
/// </summary>
public class SlotDefinition
{
    /// <summary>
    /// 槽位名称，唯一的标识符
    /// Slot name, unique identifier
    /// </summary>
    public string SlotName { get; set; } = string.Empty;

    /// <summary>
    /// 槽位描述，用于向用户询问该槽位的值
    /// Slot description, used to ask the user for the slot value
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 槽位数据类型
    /// Slot data type
    /// </summary>
    public SlotType Type { get; set; }

    /// <summary>
    /// 是否为必填槽位
    /// Whether the slot is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 是否有默认值
    /// Whether there is a default value
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// 默认值（当用户未提供时使用）
    /// Default value (used when user does not provide one)
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 同义词列表，用于识别用户输入的不同表达方式
    /// List of synonyms, used to recognize different expressions in user input
    /// 例如：对于"设备类型"槽位，同义词可能包括 ["电器", "智能设备", "装置"]
    /// </summary>
    public List<string> Synonyms { get; set; } = new();

    /// <summary>
    /// 有效值列表（仅当 Type 为 Enumeration 时使用）
    /// List of valid values (only used when Type is Enumeration)
    /// 例如：对于"设备状态"槽位，有效值可能为 ["开", "关", "暂停"]
    /// </summary>
    public string[]? ValidValues { get; set; }

    /// <summary>
    /// 依赖级别，用于确定槽位填充的优先级
    /// Dependency level, used to determine priority of slot filling
    /// 值越小优先级越高，0 表示最高优先级
    /// Smaller values indicate higher priority, 0 means highest priority
    /// </summary>
    public int DependencyLevel { get; set; }
}
