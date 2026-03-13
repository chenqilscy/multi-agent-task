using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Providers
{
    /// <summary>
    /// 空调控制实体模式提供者
    /// 专门处理空调控制相关的实体提取
    /// </summary>
    public class ACControlEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public ACControlEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Room"] = ["客厅", "卧室", "厨房", "书房", "餐厅"],
                ["Device"] = ["空调", "冷气", "暖气", "空调器"],
                ["Action"] = ["打开", "关闭", "调节", "设置", "开启", "停止"],
                ["Temperature"] = ["度", "摄氏度", "温度", "°C", "℃"],
                ["Mode"] = ["制冷", "制热", "除湿", "自动", "送风", "节能"]
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
            return @"输入：""把卧室空调调到26度""
输出：{""Room"": ""卧室"", ""Device"": ""空调"", ""Action"": ""调节"", ""Temperature"": ""26""}

输入：""打开客厅空调制冷模式""
输出：{""Room"": ""客厅"", ""Device"": ""空调"", ""Action"": ""打开"", ""Mode"": ""制冷""}

输入：""将书房空调设置到制热24度""
输出：{""Room"": ""书房"", ""Device"": ""空调"", ""Action"": ""设置"", ""Temperature"": ""24"", ""Mode"": ""制热""}";
        }
    }
}
