using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 智能家居意图能力提供者
    /// 定义智能家居场景的意图到能力的映射关系
    /// </summary>
    public class SmartHomeIntentCapabilityProvider : IIntentCapabilityProvider
    {
        private readonly Dictionary<string, string> _intentCapabilityMap;

        public SmartHomeIntentCapabilityProvider()
        {
            // 智能家居场景的意图-能力映射
            _intentCapabilityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ControlLight"] = "lighting",
                ["AdjustClimate"] = "climate",
                ["PlayMusic"] = "music",
                ["SecurityControl"] = "security",
                ["GeneralQuery"] = "general"
            };
        }

        /// <inheritdoc />
        public string? GetCapability(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
                return null;

            _intentCapabilityMap.TryGetValue(intent, out var capability);
            return capability;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSupportedIntents()
        {
            return _intentCapabilityMap.Keys;
        }
    }
}
