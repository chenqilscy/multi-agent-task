namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图到能力的映射提供者接口
    /// 具体应用实现此接口以提供业务特定的意图-能力映射
    /// </summary>
    public interface IIntentCapabilityProvider
    {
        /// <summary>
        /// 根据意图获取所需的能力
        /// </summary>
        /// <param name="intent">意图名称</param>
        /// <returns>能力名称，如果意图未映射则返回null</returns>
        string? GetCapability(string intent);

        /// <summary>
        /// 获取所有支持的意图列表
        /// </summary>
        IEnumerable<string> GetSupportedIntents();
    }
}
