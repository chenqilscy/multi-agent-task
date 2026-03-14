using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Providers
{
    /// <summary>
    /// 天气查询实体模式提供者
    /// 专门处理天气查询相关的实体提取
    /// </summary>
    public class WeatherEntityPatternProvider : IEntityPatternProvider
    {
        private readonly Dictionary<string, string[]> _entityPatternMap;

        public WeatherEntityPatternProvider()
        {
            _entityPatternMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["City"] =
                [
                    "北京", "上海", "广州", "深圳", "成都", "杭州", "南京", "武汉",
                    "重庆", "西安", "苏州", "天津", "郑州", "长沙", "东莞", "沈阳",
                    "青岛", "合肥", "福州", "济南", "昆明", "哈尔滨", "大连",
                ],
                ["Date"] =
                [
                    "今天", "明天", "后天", "今日", "明日", "周一", "周二", "周三",
                    "周四", "周五", "周六", "周日", "这周", "本周", "未来几天", "近期",
                ],
                ["QueryAspect"] =
                [
                    "天气", "气温", "温度", "降雨", "下雨", "晴", "风", "湿度",
                    "空气", "AQI", "雪", "台风", "天气预报", "穿什么", "带不带伞",
                ],
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
            return @"输入：""今天北京天气怎么样？""
输出：{""City"": ""北京"", ""Date"": ""今天"", ""QueryAspect"": ""天气""}

输入：""明天上海会下雨吗？""
输出：{""City"": ""上海"", ""Date"": ""明天"", ""QueryAspect"": ""降雨""}

输入：""天气如何？""
输出：{""Date"": ""今天"", ""QueryAspect"": ""天气""}

输入：""成都后天温度多少？""
输出：{""City"": ""成都"", ""Date"": ""后天"", ""QueryAspect"": ""气温""}";
        }
    }
}
