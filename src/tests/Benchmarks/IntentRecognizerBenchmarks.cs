using BenchmarkDotNet.Attributes;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.NLP;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MAF.Benchmarks;

/// <summary>
/// 意图识别器性能基准测试（基于规则引擎）
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class IntentRecognizerBenchmarks
{
    private RuleBasedIntentRecognizer _recognizer = null!;
    private string[] _testInputs = null!;

    [GlobalSetup]
    public void Setup()
    {
        var keywordProvider = new BenchmarkKeywordProvider();
        _recognizer = new RuleBasedIntentRecognizer(keywordProvider, NullLogger<RuleBasedIntentRecognizer>.Instance);

        _testInputs = new[]
        {
            "打开客厅的灯",
            "关闭卧室空调",
            "把温度调到25度",
            "客厅太暗了帮我开灯",
            "现在室内温度多少",
            "查看今天的天气",
            "设置明早7点的闹钟",
            "帮我播放音乐",
            "打开扫地机器人",
            "窗帘拉上"
        };
    }

    [Benchmark(Description = "单条简单意图识别")]
    public async Task<IntentRecognitionResult> RecognizeSingle()
    {
        return await _recognizer.RecognizeAsync("打开客厅的灯");
    }

    [Benchmark(Description = "单条复杂意图识别")]
    public async Task<IntentRecognitionResult> RecognizeComplex()
    {
        return await _recognizer.RecognizeAsync("帮我把客厅的灯打开然后把空调温度调低一些再关上卧室的窗帘");
    }

    [Benchmark(Description = "批量意图识别 (10条)")]
    public async Task<List<IntentRecognitionResult>> RecognizeBatch10()
    {
        return await _recognizer.RecognizeBatchAsync(_testInputs.ToList());
    }

    [Benchmark(Description = "批量意图识别 (100条)")]
    public async Task<List<IntentRecognitionResult>> RecognizeBatch100()
    {
        var inputs = new List<string>();
        for (int i = 0; i < 10; i++)
            inputs.AddRange(_testInputs);
        return await _recognizer.RecognizeBatchAsync(inputs);
    }

    /// <summary>
    /// 基准测试用关键词提供者
    /// </summary>
    private class BenchmarkKeywordProvider : IIntentKeywordProvider
    {
        private readonly Dictionary<string, string[]> _keywords = new()
        {
            ["LightControl"] = new[] { "灯", "开灯", "关灯", "打开", "关闭", "亮度", "调暗", "调亮" },
            ["AirConditionerControl"] = new[] { "空调", "温度", "制冷", "制热", "风速", "调到" },
            ["CurtainControl"] = new[] { "窗帘", "拉上", "拉开", "遮阳" },
            ["SensorQuery"] = new[] { "温度", "湿度", "查看", "多少", "传感器" },
            ["WeatherQuery"] = new[] { "天气", "气温", "下雨", "晴天" },
            ["AlarmControl"] = new[] { "闹钟", "定时", "提醒", "设置" },
            ["MusicControl"] = new[] { "音乐", "播放", "暂停", "下一首" },
            ["CleanerControl"] = new[] { "扫地", "机器人", "清扫", "拖地" }
        };

        public string?[]? GetKeywords(string intent) =>
            _keywords.TryGetValue(intent, out var kw) ? kw : null;

        public IEnumerable<string> GetSupportedIntents() => _keywords.Keys;
    }
}
