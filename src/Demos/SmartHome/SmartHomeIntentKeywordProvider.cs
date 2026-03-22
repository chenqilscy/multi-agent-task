using CKY.MultiAgentFramework.Core.Abstractions;

namespace CKY.MultiAgentFramework.Demos.SmartHome
{
    /// <summary>
    /// 智能家居意图关键词提供者
    /// 定义智能家居场景的意图到关键词的映射关系
    /// </summary>
    public class SmartHomeIntentKeywordProvider : IIntentKeywordProvider
    {
        private readonly Dictionary<string, string[]> _intentKeywordMap;

        public SmartHomeIntentKeywordProvider()
        {
            // 智能家居场景的意图-关键词映射
            _intentKeywordMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["ControlLight"] = ["灯", "照明", "亮", "暗", "开灯", "关灯"],
                ["AdjustClimate"] = ["温度", "空调", "冷", "热", "暖", "制冷", "制热"],
                ["PlayMusic"] = ["音乐", "播放", "歌曲", "歌", "音频"],
                ["SecurityControl"] = ["门", "锁", "安全", "门锁", "摄像头", "外出模式", "离家模式", "模拟有人", "监控"],
                ["QueryWeather"] = ["天气", "气温", "下雨", "晴天", "预报", "穿什么", "温度怎么样", "气候"],
                ["QueryTemperatureHistory"] = ["温度变化", "温度历史", "这段时间温度", "最近温度", "温度记录", "传感器"],
                ["SleepMode"] = ["睡眠模式", "睡觉模式", "晚安", "入睡", "夜间模式", "睡眠准备"],
                ["GuestMode"] = ["会客模式", "来客人", "朋友来", "派对模式", "聚会模式", "商务洽谈"],
                ["ReadingMode"] = ["阅读模式", "看书模式", "读书模式", "专注阅读"],
                ["MovieMode"] = ["电影模式", "观影模式", "看电影"],
                ["ExerciseMode"] = ["健身模式", "运动模式", "锻炼模式"],
                ["WorkMode"] = ["工作模式", "学习模式", "办公模式", "专注模式"],
                ["DinnerMode"] = ["聚餐模式", "晚餐模式", "用餐模式", "聚餐准备"],
                ["GeneralQuery"] = ["查询", "状态", "怎么", "什么", "帮我"]
            };
        }

        /// <inheritdoc />
        public string?[]? GetKeywords(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
                return null;

            _intentKeywordMap.TryGetValue(intent, out var keywords);
            return keywords;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSupportedIntents()
        {
            return _intentKeywordMap.Keys;
        }
    }
}
