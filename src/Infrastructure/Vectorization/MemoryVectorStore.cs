using System.Collections.Concurrent;
using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Infrastructure.Vectorization
{
    /// <summary>
    /// 内存向量存储实现（用于开发和测试）
    /// </summary>
    public class MemoryVectorStore : IVectorStore
    {
        private readonly ConcurrentDictionary<string, List<VectorPoint>> _collections = new();

        /// <inheritdoc />
        public Task CreateCollectionAsync(
            string collectionName,
            int vectorSize,
            CancellationToken ct = default)
        {
            _collections.GetOrAdd(collectionName, _ => new List<VectorPoint>());
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task InsertAsync(
            string collectionName,
            IEnumerable<VectorPoint> points,
            CancellationToken ct = default)
        {
            var collection = _collections.GetOrAdd(collectionName, _ => new List<VectorPoint>());
            lock (collection)
            {
                collection.AddRange(points);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<List<VectorSearchResult>> SearchAsync(
            string collectionName,
            float[] vector,
            int topK = 10,
            Dictionary<string, object>? filter = null,
            CancellationToken ct = default)
        {
            if (!_collections.TryGetValue(collectionName, out var collection))
            {
                return Task.FromResult(new List<VectorSearchResult>());
            }

            List<VectorPoint> items;
            lock (collection)
            {
                items = collection.ToList();
            }

            // 应用过滤器
            if (filter != null)
            {
                items = items.Where(p =>
                    filter.All(f => p.Metadata.TryGetValue(f.Key, out var val) && val?.ToString() == f.Value?.ToString())
                ).ToList();
            }

            // 计算余弦相似度
            var results = items
                .Select(p => new VectorSearchResult
                {
                    Id = p.Id,
                    Score = CosineSimilarity(vector, p.Vector),
                    Metadata = p.Metadata
                })
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();

            return Task.FromResult(results);
        }

        /// <inheritdoc />
        public Task DeleteAsync(
            string collectionName,
            IEnumerable<string> ids,
            CancellationToken ct = default)
        {
            if (!_collections.TryGetValue(collectionName, out var collection)) return Task.CompletedTask;

            var idSet = new HashSet<string>(ids);
            lock (collection)
            {
                collection.RemoveAll(p => idSet.Contains(p.Id));
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteCollectionAsync(
            string collectionName,
            CancellationToken ct = default)
        {
            _collections.TryRemove(collectionName, out _);
            return Task.CompletedTask;
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length == 0 || b.Length == 0 || a.Length != b.Length) return 0;

            var dot = a.Zip(b, (x, y) => x * y).Sum();
            var normA = MathF.Sqrt(a.Sum(x => x * x));
            var normB = MathF.Sqrt(b.Sum(x => x * x));

            return normA == 0 || normB == 0 ? 0 : dot / (normA * normB);
        }
    }
}
