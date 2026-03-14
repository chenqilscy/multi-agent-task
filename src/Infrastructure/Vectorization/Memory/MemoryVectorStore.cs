using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory
{
    /// <summary>
    /// 内存向量存储实现（用于测试和开发）
    /// </summary>
    public sealed class MemoryVectorStore : IVectorStore
    {
        private readonly Dictionary<string, List<VectorPoint>> _collections = new();
        private readonly ILogger<MemoryVectorStore> _logger;
        private readonly object _lock = new();

        public MemoryVectorStore(ILogger<MemoryVectorStore> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IVectorStore 实现

        public Task CreateCollectionAsync(
            string collectionName,
            int vectorSize,
            CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_collections.ContainsKey(collectionName))
                {
                    _logger.LogWarning("Collection already exists: {CollectionName}", collectionName);
                    return Task.CompletedTask;
                }

                _collections[collectionName] = new List<VectorPoint>();
                _logger.LogInformation("Created collection: {CollectionName}, VectorSize: {VectorSize}",
                    collectionName, vectorSize);
            }

            return Task.CompletedTask;
        }

        public Task InsertAsync(
            string collectionName,
            IEnumerable<VectorPoint> points,
            CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (!_collections.ContainsKey(collectionName))
                {
                    throw new InvalidOperationException($"Collection '{collectionName}' does not exist");
                }

                var collection = _collections[collectionName];
                collection.AddRange(points);

                _logger.LogDebug("Inserted {Count} points into collection: {CollectionName}",
                    points.Count(), collectionName);
            }

            return Task.CompletedTask;
        }

        public Task<List<VectorSearchResult>> SearchAsync(
            string collectionName,
            float[] vector,
            int topK = 10,
            Dictionary<string, object>? filter = null,
            CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (!_collections.ContainsKey(collectionName))
                {
                    throw new InvalidOperationException($"Collection '{collectionName}' does not exist");
                }

                var collection = _collections[collectionName];

                // 计算余弦相似度
                var results = collection
                    .Select(point => new
                    {
                        Point = point,
                        Score = CosineSimilarity(vector, point.Vector)
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => new VectorSearchResult
                    {
                        Id = x.Point.Id,
                        Score = x.Score,
                        Metadata = x.Point.Metadata
                    })
                    .ToList();

                return Task.FromResult(results);
            }
        }

        public Task DeleteAsync(
            string collectionName,
            IEnumerable<string> ids,
            CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (!_collections.ContainsKey(collectionName))
                {
                    throw new InvalidOperationException($"Collection '{collectionName}' does not exist");
                }

                var collection = _collections[collectionName];
                var idSet = ids.ToHashSet();

                collection.RemoveAll(point => idSet.Contains(point.Id));

                _logger.LogDebug("Deleted points from collection: {CollectionName}, Remaining: {Count}",
                    collectionName, collection.Count);
            }

            return Task.CompletedTask;
        }

        public Task DeleteCollectionAsync(
            string collectionName,
            CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (!_collections.Remove(collectionName))
                {
                    _logger.LogWarning("Collection does not exist: {CollectionName}", collectionName);
                }

                _logger.LogInformation("Deleted collection: {CollectionName}", collectionName);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region 私有辅助方法

        private static float CosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vector dimensions must match");

            float dot = 0;
            float mag1 = 0;
            float mag2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dot += vector1[i] * vector2[i];
                mag1 += vector1[i] * vector1[i];
                mag2 += vector2[i] * vector2[i];
            }

            mag1 = (float)Math.Sqrt(mag1);
            mag2 = (float)Math.Sqrt(mag2);

            if (mag1 == 0 || mag2 == 0)
                return 0;

            return dot / (mag1 * mag2);
        }

        #endregion
    }
}
