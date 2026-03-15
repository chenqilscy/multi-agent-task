using CKY.MultiAgentFramework.Core.Resilience;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Resilience
{
    /// <summary>
    /// 降级管理器实现
    /// 根据功能名称和当前降级级别控制功能开关
    /// </summary>
    public class DegradationManager : IDegradationManager
    {
        private readonly ILogger<DegradationManager> _logger;
        private volatile DegradationLevel _currentLevel = DegradationLevel.Normal;

        /// <summary>功能 → 最低可用降级级别映射（级别 >= 阈值时禁用）</summary>
        private static readonly Dictionary<string, DegradationLevel> FeatureThresholds = new()
        {
            ["recommendations"] = DegradationLevel.Level1,
            ["vector_search"] = DegradationLevel.Level2,
            ["l2_cache"] = DegradationLevel.Level3,
            ["llm_premium"] = DegradationLevel.Level4,
            ["llm"] = DegradationLevel.Level5,
        };

        public DegradationManager(ILogger<DegradationManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DegradationLevel CurrentLevel => _currentLevel;

        public void SetLevel(DegradationLevel level)
        {
            var previous = _currentLevel;
            _currentLevel = level;

            if (previous != level)
            {
                _logger.LogWarning(
                    "降级级别变更: {Previous} → {Current}",
                    previous, level);
            }
        }

        public bool IsFeatureEnabled(string feature)
        {
            if (!FeatureThresholds.TryGetValue(feature, out var threshold))
                return true; // 未注册的功能默认启用

            return _currentLevel < threshold;
        }
    }
}
