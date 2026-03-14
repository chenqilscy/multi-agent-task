using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Agents
{
    /// <summary>
    /// 温度历史查询 Agent（案例2）
    /// 负责查询室内传感器历史数据并进行趋势分析
    /// </summary>
    public class TemperatureHistoryAgent : MafBusinessAgentBase
    {
        private readonly ISensorDataService _sensorDataService;

        public override string AgentId => "temperature-history-agent-001";
        public override string Name => "TemperatureHistoryAgent";
        public override string Description => "室内温度历史查询 Agent，支持查询传感器历史数据并进行趋势分析";
        public override IReadOnlyList<string> Capabilities =>
            ["temperature-history", "sensor-data", "trend-analysis"];

        public TemperatureHistoryAgent(
            ISensorDataService sensorDataService,
            IMafAiAgentRegistry llmRegistry,
            ILogger<TemperatureHistoryAgent> logger)
            : base(llmRegistry, logger)
        {
            _sensorDataService = sensorDataService
                ?? throw new ArgumentNullException(nameof(sensorDataService));
        }

        private const int MaxHistoryDays = 90;  // 传感器数据保留最长90天

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            // 从参数获取房间（必需）和时间范围（可选，默认7天）
            string room = request.Parameters.TryGetValue("room", out var r)
                ? r?.ToString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(room))
            {
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    NeedsClarification = true,
                    ClarificationQuestion = "请问您想查询哪个房间的温度历史？",
                    ClarificationOptions = ["客厅", "卧室", "厨房", "书房"],
                };
            }

            // 解析时间范围
            int days = ParseDays(request.UserInput, request.Parameters);
            Logger.LogInformation(
                "TemperatureHistoryAgent querying {Room} temperature history for {Days} days",
                room, days);

            try
            {
                var records = await _sensorDataService.GetTemperatureHistoryAsync(room, days, ct);

                if (records.Count == 0)
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Result = $"抱歉，{room}暂无温度历史数据",
                    };
                }

                var analysis = AnalyzeTemperatureHistory(records, room, days);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = analysis.Summary,
                    Data = new { Records = records, Analysis = analysis },
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "TemperatureHistoryAgent failed for room {Room}", room);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = ex.Message,
                    Result = $"抱歉，查询{room}温度历史失败，请稍后重试",
                };
            }
        }

        private static int ParseDays(string userInput, Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("days", out var dObj) && dObj != null)
            {
                if (int.TryParse(dObj.ToString(), out var d) && d > 0 && d <= MaxHistoryDays)
                    return d;
            }

            // 从自然语言中推断
            if (userInput.Contains("一周") || userInput.Contains("7天"))
                return 7;
            if (userInput.Contains("两周") || userInput.Contains("14天"))
                return 14;
            if (userInput.Contains("一个月") || userInput.Contains("30天"))
                return 30;
            if (userInput.Contains("三天") || userInput.Contains("3天"))
                return 3;

            // 默认7天
            return 7;
        }

        // 稳定性判断阈值（°C）：温差小于此值视为基本稳定
        private const double StabilityThresholdCelsius = 0.5;

        private static TemperatureAnalysis AnalyzeTemperatureHistory(
            List<TemperatureRecord> records, string room, int days)
        {
            var temps = records.Select(r => r.Temperature).ToList();
            double minTemp = temps.Min();
            double maxTemp = temps.Max();
            double avgTemp = Math.Round(temps.Average(), 1);

            // 趋势分析：对比前半段和后半段均值
            int half = records.Count / 2;
            double firstHalfAvg = records.Take(half).Average(r => r.Temperature);
            double secondHalfAvg = records.Skip(half).Average(r => r.Temperature);
            double diff = secondHalfAvg - firstHalfAvg;

            string trend = Math.Abs(diff) < StabilityThresholdCelsius ? "基本稳定"
                : diff > 0 ? $"呈上升趋势（近期升高约 {Math.Abs(diff):F1}°C）"
                : $"呈下降趋势（近期降低约 {Math.Abs(diff):F1}°C）";

            // 最高/最低温对应时间
            var maxRecord = records.MaxBy(r => r.Temperature)!;
            var minRecord = records.MinBy(r => r.Temperature)!;

            // 生成建议
            string advice = avgTemp > 26
                ? "💡 建议：室内温度偏高，建议开启制冷模式或通风降温。"
                : avgTemp < 18
                ? "💡 建议：室内温度偏低，建议开启制热模式或空调供暖。"
                : diff > 2
                ? "💡 建议：近期温度明显上升，如需保持舒适，可开启空调恒温。"
                : diff < -2
                ? "💡 建议：近期温度明显下降，注意保暖，可适当调高空调设定温度。"
                : "💡 建议：温度波动不大，空调可设为自动模式保持舒适。";

            var summary = new List<string>
            {
                $"🌡️ {room} 过去 {days} 天温度变化情况：",
                $"• 平均温度：{avgTemp}°C",
                $"• 温度范围：{minTemp}°C ~ {maxTemp}°C",
                $"• 变化趋势：{trend}",
                $"• 最高温：{maxTemp}°C（{maxRecord.Timestamp:MM月dd日 HH时}）",
                $"• 最低温：{minTemp}°C（{minRecord.Timestamp:MM月dd日 HH时}）",
                "",
                advice,
            };

            return new TemperatureAnalysis
            {
                Room = room,
                Days = days,
                MinTemperature = minTemp,
                MaxTemperature = maxTemp,
                AvgTemperature = avgTemp,
                Trend = trend,
                Summary = string.Join("\n", summary),
            };
        }
    }

    /// <summary>
    /// 温度趋势分析结果
    /// </summary>
    public class TemperatureAnalysis
    {
        public string Room { get; set; } = string.Empty;
        public int Days { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public double AvgTemperature { get; set; }
        public string Trend { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
