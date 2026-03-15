namespace CKY.MultiAgentFramework.Core.Models.Dialog;

/// <summary>
/// 意图槽位定义，描述一个意图所需的所有槽位
/// Intent slot definition, describing all slots required for an intent
/// </summary>
public class IntentSlotDefinition
{
    /// <summary>
    /// 意图名称或标识符
    /// Intent name or identifier
    /// 例如："TurnOnDevice", "SetTemperature", "PlayMusic"
    /// </summary>
    public string Intent { get; set; } = string.Empty;

    /// <summary>
    /// 必填槽位列表
    /// List of required slots
    /// 这些槽位必须被填充才能执行该意图
    /// These slots must be filled before the intent can be executed
    /// </summary>
    public List<SlotDefinition> RequiredSlots { get; set; } = new();

    /// <summary>
    /// 可选槽位列表
    /// List of optional slots
    /// 这些槽位可以增强意图执行效果，但不是必需的
    /// These slots can enhance intent execution but are not required
    /// </summary>
    public List<SlotDefinition> OptionalSlots { get; set; } = new();

    /// <summary>
    /// 获取所有槽位（必填 + 可选）
    /// Get all slots (required + optional)
    /// </summary>
    /// <returns>所有槽位的列表</returns>
    public List<SlotDefinition> GetAllSlots()
    {
        var allSlots = new List<SlotDefinition>(RequiredSlots);
        allSlots.AddRange(OptionalSlots);
        return allSlots;
    }

    /// <summary>
    /// 根据槽位名称查找槽位定义
    /// Find slot definition by slot name
    /// </summary>
    /// <param name="slotName">槽位名称</param>
    /// <returns>找到的槽位定义，如果未找到则返回 null</returns>
    public SlotDefinition? FindSlot(string slotName)
    {
        return RequiredSlots.FirstOrDefault(s => s.SlotName == slotName)
            ?? OptionalSlots.FirstOrDefault(s => s.SlotName == slotName);
    }

    /// <summary>
    /// 检查槽位是否为必填
    /// Check if a slot is required
    /// </summary>
    /// <param name="slotName">槽位名称</param>
    /// <returns>如果槽位是必填的返回 true，否则返回 false</returns>
    public bool IsSlotRequired(string slotName)
    {
        return RequiredSlots.Any(s => s.SlotName == slotName);
    }
}
