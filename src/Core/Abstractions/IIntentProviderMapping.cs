namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图到实体模式提供者的映射接口
    /// 由业务层注册映射关系
    /// </summary>
    public interface IIntentProviderMapping
    {
        /// <summary>
        /// 注册意图到 Provider 类型的映射
        /// </summary>
        void Register(string intent, Type providerType);

        /// <summary>
        /// 获取意图对应的 Provider 类型
        /// </summary>
        Type? GetProviderType(string intent);

        /// <summary>
        /// 获取所有已注册的意图
        /// </summary>
        IEnumerable<string> GetRegisteredIntents();
    }
}
