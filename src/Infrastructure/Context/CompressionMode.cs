namespace CKY.MultiAgentFramework.Infrastructure.Context
{
    /// <summary>
    /// 压缩模式
    /// </summary>
    public enum CompressionMode
    {
        /// <summary>
        /// 简单模式：仅截断，不总结
        /// </summary>
        Simple,

        /// <summary>
        /// 智能模式：使用 LLM 总结
        /// </summary>
        Smart
    }
}
