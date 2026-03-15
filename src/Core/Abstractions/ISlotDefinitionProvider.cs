using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Core.Abstractions;

/// <summary>
/// 槽位定义提供者接口，负责管理意图与槽位定义的映射关系
/// Slot definition provider interface, responsible for managing intent-to-slot-definition mappings
/// </summary>
public interface ISlotDefinitionProvider
{
    /// <summary>
    /// 获取指定意图的槽位定义
    /// Get slot definition for a specific intent
    /// </summary>
    /// <param name="intent">意图名称</param>
    /// <returns>意图槽位定义，如果未找到则返回 null</returns>
    IntentSlotDefinition? GetDefinition(string intent);

    /// <summary>
    /// 获取所有意图的槽位定义字典
    /// Get all intent slot definitions as a dictionary
    /// </summary>
    /// <returns>键为意图名称，值为意图槽位定义的字典</returns>
    Dictionary<string, IntentSlotDefinition> GetAllDefinitions();
}
