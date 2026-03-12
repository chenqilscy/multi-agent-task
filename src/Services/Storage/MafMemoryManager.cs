using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Storage
{
    /// <summary>
    /// 记忆管理服务
    /// 管理系统的长期记忆，包括语义记忆和情景记忆
    /// </summary>
    public class MafMemoryManager : IMafMemoryManager
    {
        private readonly IVectorStore _vectorStore;
        private readonly IRelationalDatabase _database;
        private readonly ILogger<MafMemoryManager> _logger;

        private const string EpisodicCollectionName = "episodic_memory";
        private const int DefaultVectorSize = 1536;

        public MafMemoryManager(
            IVectorStore vectorStore,
            IRelationalDatabase database,
            ILogger<MafMemoryManager> logger)
        {
            _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<MemoryContext> GetRelevantMemoryAsync(
            string userId,
            string query,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Getting relevant memory for user {UserId}, query: {Query}", userId, query);

            var context = new MemoryContext();

            try
            {
                // 从向量数据库检索语义相关的记忆
                // 注意：实际应用中需要先将query转换为向量
                var dummyVector = new float[DefaultVectorSize];
                var searchResults = await _vectorStore.SearchAsync(
                    EpisodicCollectionName,
                    dummyVector,
                    topK: 5,
                    filter: new Dictionary<string, object> { ["userId"] = userId },
                    ct: ct);

                foreach (var result in searchResults)
                {
                    if (result.Metadata.TryGetValue("summary", out var summary))
                    {
                        context.EpisodicMemories.Add(new EpisodicMemory
                        {
                            MemoryId = result.Id,
                            Summary = summary?.ToString() ?? string.Empty,
                            Tags = result.Metadata.TryGetValue("tags", out var tags)
                                ? (List<string>)(tags ?? new List<string>())
                                : new List<string>()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve episodic memories from vector store");
            }

            return context;
        }

        /// <inheritdoc />
        public async Task SaveEpisodicMemoryAsync(
            string userId,
            string conversationId,
            string summary,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Saving episodic memory for user {UserId}", userId);

            try
            {
                // 注意：实际应用中需要将summary转换为向量
                var dummyVector = new float[DefaultVectorSize];
                var point = new VectorPoint
                {
                    Id = Guid.NewGuid().ToString(),
                    Vector = dummyVector,
                    Metadata = new Dictionary<string, object>
                    {
                        ["userId"] = userId,
                        ["conversationId"] = conversationId,
                        ["summary"] = summary,
                        ["timestamp"] = DateTime.UtcNow.ToString("O")
                    }
                };

                await _vectorStore.InsertAsync(EpisodicCollectionName, [point], ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save episodic memory to vector store");
            }
        }

        /// <inheritdoc />
        public async Task SaveSemanticMemoryAsync(
            string userId,
            string key,
            string value,
            List<string> tags,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Saving semantic memory for user {UserId}, key: {Key}", userId, key);

            var memory = new SemanticMemory
            {
                Key = key,
                Value = value,
                Tags = tags
            };

            try
            {
                await _database.InsertAsync(memory, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save semantic memory to database");
            }
        }
    }
}
