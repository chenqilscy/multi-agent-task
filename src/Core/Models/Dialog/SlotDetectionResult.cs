namespace CKY.MultiAgentFramework.Core.Models.Dialog;

/// <summary>
/// 槽位检测结果，包含从用户输入中检测到的槽位信息
/// Slot detection result, containing slot information detected from user input
/// </summary>
public class SlotDetectionResult
{
    /// <summary>
    /// 检测到的意图
    /// Detected intent
    /// </summary>
    public string Intent { get; set; } = string.Empty;

    /// <summary>
    /// 缺失的必填槽位列表
    /// List of missing required slots
    /// 这些槽位尚未被填充，需要向用户询问
    /// These slots have not been filled yet and need to be asked from the user
    /// </summary>
    public List<SlotDefinition> MissingSlots { get; set; } = new();

    /// <summary>
    /// 可选槽位列表（包括已填充和未填充的）
    /// List of optional slots (both filled and unfilled)
    /// </summary>
    public List<SlotDefinition> OptionalSlots { get; set; } = new();

    /// <summary>
    /// 已检测到的槽位值字典
    /// Dictionary of detected slot values
    /// Key: 槽位名称 (SlotName)
    /// Value: 槽位值 (Slot value)
    /// </summary>
    public Dictionary<string, object> DetectedSlots { get; set; } = new();

    /// <summary>
    /// 意图识别的置信度（0.0 到 1.0）
    /// Confidence score of intent recognition (0.0 to 1.0)
    /// 1.0 表示完全确定，0.0 表示完全不确定
    /// 1.0 means completely certain, 0.0 means completely uncertain
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 检查所有必填槽位是否都已填充
    /// Check if all required slots are filled
    /// </summary>
    /// <returns>如果所有必填槽位都已填充返回 true，否则返回 false</returns>
    public bool AreRequiredSlotsFilled()
    {
        return MissingSlots.Count == 0;
    }

    /// <summary>
    /// 获取指定槽位的值
    /// Get the value of a specific slot
    /// </summary>
    /// <param name="slotName">槽位名称</param>
    /// <returns>槽位值，如果槽位不存在则返回 null</returns>
    public object? GetSlotValue(string slotName)
    {
        return DetectedSlots.TryGetValue(slotName, out var value) ? value : null;
    }

    /// <summary>
    /// 设置槽位值
    /// Set slot value
    /// </summary>
    /// <param name="slotName">槽位名称</param>
    /// <param name="value">槽位值</param>
    public void SetSlotValue(string slotName, object value)
    {
        DetectedSlots[slotName] = value;
    }

    /// <summary>
    /// 获取缺失槽位的名称列表
    /// Get list of missing slot names
    /// </summary>
    /// <returns>缺失槽位名称的列表</returns>
    public List<string> GetMissingSlotNames()
    {
        return MissingSlots.Select(s => s.SlotName).ToList();
    }
}
