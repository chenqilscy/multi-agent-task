using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Core.Abstractions;

/// <summary>
/// 槽位管理器接口，负责检测、填充和管理对话槽位
/// Slot manager interface, responsible for detecting, filling, and managing dialog slots
/// </summary>
public interface ISlotManager
{
    /// <summary>
    /// 检测缺失的必填槽位
    /// Detect missing required slots
    /// </summary>
    /// <param name="userInput">用户输入文本</param>
    /// <param name="intent">意图识别结果</param>
    /// <param name="entities">实体提取结果</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>槽位检测结果，包含缺失的槽位和已检测到的槽位值</returns>
    Task<SlotDetectionResult> DetectMissingSlotsAsync(
        string userInput,
        IntentRecognitionResult intent,
        EntityExtractionResult entities,
        CancellationToken ct = default);

    /// <summary>
    /// 填充槽位值
    /// Fill slot values
    /// </summary>
    /// <param name="intent">意图名称</param>
    /// <param name="providedSlots">用户提供的槽位值字典</param>
    /// <param name="context">对话上下文（用于指代消解、默认值推断等）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>填充后的完整槽位字典</returns>
    Task<Dictionary<string, object>> FillSlotsAsync(
        string intent,
        Dictionary<string, object> providedSlots,
        DialogContext context,
        CancellationToken ct = default);

    /// <summary>
    /// 生成澄清问题，用于向用户询问缺失的槽位值
    /// Generate clarification questions to ask the user for missing slot values
    /// </summary>
    /// <param name="missingSlots">缺失的槽位列表</param>
    /// <param name="intent">意图名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>生成的澄清问题文本</returns>
    Task<string> GenerateClarificationAsync(
        List<SlotDefinition> missingSlots,
        string intent,
        CancellationToken ct = default);
}
