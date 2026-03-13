using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Providers
{
    /// <summary>
    /// 灯光控制实体模式提供者
    /// 专门处理灯光控制相关的实体提取
    /// </summary>
    public class LightControlEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public LightControlEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = ["客厅", "卧室", "厨房", "浴室", "书房", "餐厅", "阳台", "走廊"],
                ["Device"] = ["灯", "照明", "吊灯", "台灯", "筒灯", "灯带", "主灯", "辅灯"],
                ["Action"] = ["打开", "关闭", "调节", "调亮", "调暗", "增加亮度", "减少亮度"],
                ["Brightness"] = ["亮度", "明亮度", "光强", "光照"]
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
            return @"输入：""打开客厅的灯""
输出：{""Room"": ""客厅"", ""Device"": ""灯"", ""Action"": ""打开""}

输入：""把卧室灯调暗一点""
输出：{""Room"": ""卧室"", ""Device"": ""灯"", ""Action"": ""调暗""}

输入：""将书房台灯亮度调到80%""
输出：{""Room"": ""书房"", ""Device"": ""台灯"", ""Action"": ""调节"", ""Brightness"": ""80%""}";
        }
    }
}
