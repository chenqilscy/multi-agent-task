using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.NLP
{
    /// <summary>
    /// 实体提取服务
    /// 从用户输入中提取关键实体（房间、设备、数值等）
    /// </summary>
    public class MafEntityExtractor : IEntityExtractor
    {
        private readonly ILogger<MafEntityExtractor> _logger;
        private readonly IEntityPatternProvider _patternProvider;

        public MafEntityExtractor(
            IEntityPatternProvider patternProvider,
            ILogger<MafEntityExtractor> logger)
        {
            _patternProvider = patternProvider ?? throw new ArgumentNullException(nameof(patternProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<EntityExtractionResult> ExtractAsync(
            string userInput,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Extracting entities from: {Input}", userInput);

            var result = new EntityExtractionResult();
            var supportedTypes = _patternProvider.GetSupportedEntityTypes();

            foreach (var entityType in supportedTypes)
            {
                var patterns = _patternProvider.GetPatterns(entityType);
                if (patterns != null)
                {
                    foreach (var pattern in patterns)
                    {
                        if (!string.IsNullOrEmpty(pattern))
                        {
                            var index = userInput.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0)
                            {
                                var entity = new Entity
                                {
                                    EntityType = entityType,
                                    EntityValue = pattern,
                                    StartPosition = index,
                                    EndPosition = index + pattern.Length,
                                    Confidence = 0.9
                                };
                                result.ExtractedEntities.Add(entity);

                                if (!result.Entities.ContainsKey(entityType))
                                {
                                    result.Entities[entityType] = pattern;
                                }
                            }
                        }
                    }
                }
            }

            return Task.FromResult(result);
        }
    }
}
