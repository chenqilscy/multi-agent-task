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

        private static readonly Dictionary<string, string[]> EntityPatterns = new()
        {
            ["Room"] = ["客厅", "卧室", "厨房", "浴室", "书房", "餐厅", "阳台"],
            ["Device"] = ["灯", "空调", "窗帘", "音箱", "电视", "门锁", "摄像头"],
            ["Action"] = ["打开", "关闭", "调节", "设置", "增加", "减少", "播放", "暂停"]
        };

        public MafEntityExtractor(ILogger<MafEntityExtractor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<EntityExtractionResult> ExtractAsync(
            string userInput,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Extracting entities from: {Input}", userInput);

            var result = new EntityExtractionResult();

            foreach (var (entityType, patterns) in EntityPatterns)
            {
                foreach (var pattern in patterns)
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

            return Task.FromResult(result);
        }
    }
}
