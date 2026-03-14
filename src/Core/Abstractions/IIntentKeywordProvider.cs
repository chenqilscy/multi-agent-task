namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图关键词提供者接口
    /// 用于解耦框架层与具体业务场景的关键词定义
    /// </summary>
    public interface IIntentKeywordProvider
    {
        /// <summary>
        /// 获取指定意图的关键词列表
        /// </summary>
        /// <param name="intent">意图名称</param>
        /// <returns>关键词数组，如果意图不存在则返回 null</returns>
        string?[]? GetKeywords(string intent);

        /// <summary>
        /// 获取所有支持的意图名称
        /// </summary>
        IEnumerable<string> GetSupportedIntents();
    }
}
