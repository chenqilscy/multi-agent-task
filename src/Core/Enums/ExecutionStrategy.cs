namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 执行策略枚举
    /// </summary>
    public enum ExecutionStrategy
    {
        /// <summary>立即执行</summary>
        Immediate,
        /// <summary>并行执行</summary>
        Parallel,
        /// <summary>串行执行</summary>
        Serial,
        /// <summary>延迟执行</summary>
        Delayed,
        /// <summary>条件执行</summary>
        Conditional
    }
}
