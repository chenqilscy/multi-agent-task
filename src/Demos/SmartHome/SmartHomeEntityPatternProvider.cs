using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 智能家居实体模式提供者
    /// 定义智能家居场景的实体类型和模式
    /// </summary>
    public class SmartHomeEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public SmartHomeEntityPatternProvider()
        {
            // 智能家居场景的实体类型-模式映射
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = ["客厅", "卧室", "厨房", "浴室", "书房", "餐厅", "阳台"],
                ["Device"] = ["灯", "空调", "窗帘", "音箱", "电视", "门锁", "摄像头"],
                ["Action"] = ["打开", "关闭", "调节", "设置", "增加", "减少", "播放", "暂停"]
            };
        }

        /// <inheritdoc />
        public string?[]? GetPatterns(string entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return null;

            _entityPatternMap.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _entityPatternMap.Keys;
        }

        /// <summary>
        /// 获取 Few-shot 示例（用于 LLM Prompt）
        /// 由业务层实现，提供该场景的示例
        /// 注意：输出格式使用双引号表示JSON结构，便于LLM理解
        /// </summary>
        public string GetFewShotExamples()
        {
            return @"输入：""打开客厅的灯""
输出：{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""打开""}

输入：""把卧室空调调到26度""
输出：{""Room"": ""卧室"", ""Device"": ""空调"", ""Action"": ""调节"", ""Value"": ""26""}";
        }
    }
}
