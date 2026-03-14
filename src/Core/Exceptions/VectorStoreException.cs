namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// 向量存储异常（向量数据库操作失败）
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 向量索引创建失败（配置错误、资源不足）
    /// - 向量相似度搜索失败（索引不存在、查询超时）
    /// - 向量插入/更新失败（维度不匹配、数据过大）
    /// - 向量删除失败（文档不存在、权限不足）
    /// - 向量序列化/反序列化失败
    ///
    /// 错误处理建议：
    /// - IsRetryable = true: 启用重试机制（网络抖动、临时性故障）
    /// - 维度不匹配: 检查向量配置和嵌入模型
    /// - 索引不存在: 自动创建索引或降级到关键词搜索
    ///
    /// 设计原则：
    /// - 向量搜索失败应优雅降级到关键词搜索
    /// - 记录详细错误日志用于诊断
    /// - 不影响主流程的可用性
    ///
    /// 示例：
    /// <code>
    /// try
    /// {
    ///     var results = await _vectorStore.SearchAsync(embedding, topK: 10);
    /// }
    /// catch (VectorStoreException ex)
    /// {
    ///     _logger.LogWarning(ex, "向量搜索失败，降级到关键词搜索");
    ///     // 降级到关键词搜索
    ///     results = await _keywordSearch.SearchAsync(query, topK: 10);
    /// }
    /// </code>
    /// </remarks>
    public class VectorStoreException : MafException
    {
        /// <summary>
        /// 初始化 VectorStoreException 类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        public VectorStoreException(string message)
            : base(MafErrorCode.VectorStoreError, message, isRetryable: true, component: "VectorStore")
        {
        }
    }
}
