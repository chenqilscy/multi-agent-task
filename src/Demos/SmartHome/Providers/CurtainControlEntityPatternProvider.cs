using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Providers
{
    /// <summary>
    /// 窗帘控制实体模式提供者
    /// 专门处理窗帘控制相关的实体提取
    /// </summary>
    public class CurtainControlEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public CurtainControlEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = ["客厅", "卧室", "厨房", "书房", "餐厅"],
                ["Device"] = ["窗帘", "百叶窗", "卷帘", "遮光帘", "纱帘"],
                ["Action"] = ["打开", "关闭", "拉开", "拉上", "升起", "降下", "调节"],
                ["Position"] = ["一半", "全开", "全关", "50%", "100%", "0%", "中间位置"]
            };
        }

        public string?[]? GetPatterns(string entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return null;

            _entityPatternMap.TryGetValue(entityType, out var patterns);
            return patterns;
        }

        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return _entityPatternMap.Keys;
        }

        public string GetFewShotExamples()
        {
            return @"输入：""打开客厅窗帘""
输出：{""Room"": ""客厅"", ""Device"": ""窗帘"", ""Action"": ""打开""}

输入：""把卧室窗帘拉到一半""
输出：{""Room"": ""卧室"", ""Device"": ""窗帘"", ""Action"": ""调节"", ""Position"": ""一半""}

输入：""将书房窗帘全关上""
输出：{""Room"": ""书房"", ""Device"": ""窗帘"", ""Action"": ""关闭"", ""Position"": ""全关""}";
        }
    }
}
