namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 指代消解器接口
    /// 解决多轮对话中的指代关系，将"它"、"那个"等代词替换为实际实体
    /// </summary>
    public interface ICoreferenceResolver
    {
        /// <summary>
        /// 消解指代词
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <param name="conversationId">对话ID，用于检索历史上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>消解后的文本</returns>
        Task<string> ResolveAsync(
            string userInput,
            string conversationId,
            CancellationToken ct = default);
    }
}
