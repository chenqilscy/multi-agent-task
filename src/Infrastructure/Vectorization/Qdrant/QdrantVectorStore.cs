using CKY.MultiAgentFramework.Core.Abstractions;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant
{
    /// <summary>
    /// Qdrant 向量存储实现
    /// </summary>
    public sealed class QdrantVectorStore : IVectorStore
    {
        private readonly QdrantClient _client;
        private readonly ILogger<QdrantVectorStore> _logger;
        private readonly QdrantVectorStoreOptions _options;

        public QdrantVectorStore(
            QdrantClient client,
            ILogger<QdrantVectorStore> logger,
            IOptions<QdrantVectorStoreOptions> options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new QdrantVectorStoreOptions();

            _logger.LogInformation("QdrantVectorStore initialized (Host: {Host}:{Port})",
                _options.Host, _options.Port);
        }

        #region IVectorStore 实现

        public async Task CreateCollectionAsync(
            string collectionName,
            int vectorSize,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Creating collection: {CollectionName}, VectorSize: {VectorSize}",
                    collectionName, vectorSize);

                await _client.CreateCollectionAsync(
                    collectionName,
                    new VectorParams
                    {
                        Size = (ulong)vectorSize,
                        Distance = ParseDistanceMetric(_options.DistanceMetric)
                    },
                    cancellationToken: ct);

                _logger.LogInformation("Collection created successfully: {CollectionName}", collectionName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to create collection: {CollectionName}", collectionName);
                throw;
            }
        }

        public async Task InsertAsync(
            string collectionName,
            IEnumerable<VectorPoint> points,
            CancellationToken ct = default)
        {
            try
            {
                var pointList = points.ToList();
                _logger.LogInformation("Inserting {Count} points into collection: {CollectionName}",
                    pointList.Count, collectionName);

                var qdrantPoints = new List<PointStruct>();
                foreach (var point in pointList)
                {
                    var pointId = ParsePointId(point.Id);
                    var payloadDict = ConvertMetadataToPayload(point.Metadata);

                    var pointStruct = new PointStruct
                    {
                        Id = pointId,
                        Vectors = point.Vector,
                        Payload = { payloadDict }
                    };
                    qdrantPoints.Add(pointStruct);
                }

                await _client.UpsertAsync(
                    collectionName,
                    qdrantPoints,
                    wait: true,
                    cancellationToken: ct);

                _logger.LogInformation("Successfully inserted {Count} points", qdrantPoints.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to insert points into collection: {CollectionName}", collectionName);
                throw;
            }
        }

        public async Task<List<VectorSearchResult>> SearchAsync(
            string collectionName,
            float[] vector,
            int topK = 10,
            Dictionary<string, object>? filter = null,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("Searching in collection: {CollectionName}, TopK: {TopK}",
                    collectionName, topK);

                var searchFilter = BuildFilter(filter);

                var results = await _client.SearchAsync(
                    collectionName,
                    vector,
                    filter: searchFilter,
                    limit: (ulong)topK,
                    payloadSelector: new WithPayloadSelector { Enable = true },
                    cancellationToken: ct);

                var searchResults = results.Select(r => new VectorSearchResult
                {
                    Id = r.Id.Uuid.ToString(),
                    Score = r.Score,
                    Metadata = ConvertPayloadToMetadata(r.Payload)
                }).ToList();

                if (_options.EnableVerboseLogging)
                {
                    _logger.LogDebug("Found {Count} results", searchResults.Count);
                }

                return searchResults;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Search failed in collection: {CollectionName}", collectionName);
                throw;
            }
        }

        public async Task DeleteAsync(
            string collectionName,
            IEnumerable<string> ids,
            CancellationToken ct = default)
        {
            try
            {
                var idList = ids.ToList();
                _logger.LogInformation("Deleting {Count} points from collection: {CollectionName}",
                    idList.Count, collectionName);

                var pointIds = idList.Select(id =>
                {
                    if (Guid.TryParse(id, out var guid))
                        return guid;
                    throw new ArgumentException($"Invalid point ID format: '{id}'. Expected UUID format.");
                }).ToList();

                await _client.DeleteAsync(
                    collectionName,
                    pointIds,
                    wait: true,
                    cancellationToken: ct);

                _logger.LogInformation("Successfully deleted {Count} points", pointIds.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to delete points from collection: {CollectionName}", collectionName);
                throw;
            }
        }

        public async Task DeleteCollectionAsync(
            string collectionName,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Deleting collection: {CollectionName}", collectionName);

                await _client.DeleteCollectionAsync(collectionName, cancellationToken: ct);

                _logger.LogInformation("Collection deleted successfully: {CollectionName}", collectionName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to delete collection: {CollectionName}", collectionName);
                throw;
            }
        }

        #endregion

        #region 私有辅助方法

        private static Distance ParseDistanceMetric(string metric)
        {
            return metric.ToUpperInvariant() switch
            {
                "COSINE" => Distance.Cosine,
                "EUCLIDEAN" => Distance.Euclid,
                "DOTPRODUCT" => Distance.Dot,
                _ => Distance.Cosine
            };
        }

        private static Guid ParsePointId(string id)
        {
            if (Guid.TryParse(id, out var guid))
                return guid;
            throw new ArgumentException($"Invalid point ID format: '{id}'. Expected UUID format.");
        }

        private static Dictionary<string, Value> ConvertMetadataToPayload(Dictionary<string, object> metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return new Dictionary<string, Value>();

            var payload = new Dictionary<string, Value>();
            foreach (var kvp in metadata)
            {
                payload[kvp.Key] = ConvertToValue(kvp.Value);
            }
            return payload;
        }

        private static Value ConvertToValue(object? value)
        {
            return value switch
            {
                null => new Value { StringValue = "" },
                bool b => new Value { BoolValue = b },
                int i => new Value { IntegerValue = i },
                long l => new Value { IntegerValue = l },
                float f => new Value { DoubleValue = f },
                double d => new Value { DoubleValue = d },
                _ => new Value { StringValue = value.ToString() ?? "" }
            };
        }

        private static Dictionary<string, object> ConvertPayloadToMetadata(IReadOnlyDictionary<string, Value> payload)
        {
            var metadata = new Dictionary<string, object>();

            foreach (var kvp in payload)
            {
                metadata[kvp.Key] = kvp.Value.KindCase switch
                {
                    Value.KindOneofCase.BoolValue => kvp.Value.BoolValue,
                    Value.KindOneofCase.IntegerValue => kvp.Value.IntegerValue,
                    Value.KindOneofCase.DoubleValue => kvp.Value.DoubleValue,
                    _ => kvp.Value.StringValue ?? string.Empty
                };
            }

            return metadata;
        }

        private static Filter? BuildFilter(Dictionary<string, object>? filterDict)
        {
            if (filterDict == null || filterDict.Count == 0)
                return null;

            var conditions = new List<Condition>();
            foreach (var kvp in filterDict)
            {
                var match = kvp.Value switch
                {
                    bool b => new Match { Boolean = b },
                    int i => new Match { Integer = i },
                    long l => new Match { Integer = l },
                    _ => new Match { Keyword = kvp.Value?.ToString() ?? "" }
                };

                conditions.Add(new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = kvp.Key,
                        Match = match
                    }
                });
            }

            return new Filter { Must = { conditions } };
        }

        #endregion
    }
}
