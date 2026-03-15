namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 向量点
    /// </summary>
    public class VectorPoint
    {
        /// <summary>向量ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>向量数据</summary>
        public float[] Vector { get; set; } = Array.Empty<float>();

        /// <summary>附加元数据</summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 向量搜索结果
    /// </summary>
    public class VectorSearchResult
    {
        /// <summary>向量ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>相似度评分</summary>
        public float Score { get; set; }

        /// <summary>附加元数据</summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 向量存储抽象接口
    /// 支持语义检索和RAG（检索增强生成）
    /// </summary>
    /// <remarks>
    /// <para><b>默认推荐实现：</b>MemoryVectorStore（Demo/开发环境）</para>
    /// <para><b>生产环境推荐：</b>QdrantVectorStore</para>
    /// <para><b>实现对比：</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>实现</term>
    ///     <description>适用场景</description>
    ///   </listheader>
    ///   <item>
    ///     <term>MemoryVectorStore</term>
    ///     <description>Demo、开发测试、小规模场景（&lt; 1万向量）。零配置但不持久化。</description>
    ///   </item>
    ///   <item>
    ///     <term>QdrantVectorStore</term>
    ///     <description>生产环境、大规模场景（&gt; 10万向量）。需要 Docker 部署。</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public interface IVectorStore
    {
        /// <summary>
        /// 创建集合
        /// </summary>
        Task CreateCollectionAsync(
            string collectionName,
            int vectorSize,
            CancellationToken ct = default);

        /// <summary>
        /// 插入向量
        /// </summary>
        Task InsertAsync(
            string collectionName,
            IEnumerable<VectorPoint> points,
            CancellationToken ct = default);

        /// <summary>
        /// 相似度检索
        /// </summary>
        Task<List<VectorSearchResult>> SearchAsync(
            string collectionName,
            float[] vector,
            int topK = 10,
            Dictionary<string, object>? filter = null,
            CancellationToken ct = default);

        /// <summary>
        /// 删除向量
        /// </summary>
        Task DeleteAsync(
            string collectionName,
            IEnumerable<string> ids,
            CancellationToken ct = default);

        /// <summary>
        /// 删除集合
        /// </summary>
        Task DeleteCollectionAsync(
            string collectionName,
            CancellationToken ct = default);
    }
}
