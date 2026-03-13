namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 实体模式提供者接口
    /// 用于解耦框架层与具体业务场景的实体模式定义
    /// </summary>
    public interface IEntityPatternProvider
    {
        /// <summary>
        /// 获取指定实体类型的模式列表
        /// </summary>
        /// <param name="entityType">实体类型（如 "Room", "Device", "Action" 等）</param>
        /// <returns>模式数组，如果实体类型不存在则返回 null</returns>
        string?[]? GetPatterns(string entityType);

        /// <summary>
        /// 获取所有支持的实体类型
        /// </summary>
        IEnumerable<string> GetSupportedEntityTypes();

        /// <summary>
        /// 获取 Few-shot 示例（用于 LLM Prompt）
        /// 由业务层实现，提供该场景的示例
        /// </summary>
        string GetFewShotExamples();
    }
}
