using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Agents
{
    /// <summary>
    /// 天气查询 Agent（案例1）
    /// 负责查询天气信息，并整合穿衣、出行建议
    /// </summary>
    public class WeatherAgent : MafBusinessAgentBase
    {
        private readonly IWeatherService _weatherService;

        public override string AgentId => "weather-agent-001";
        public override string Name => "WeatherAgent";
        public override string Description => "天气查询 Agent，支持查询当天及未来几天天气，并提供穿衣出行建议";
        public override IReadOnlyList<string> Capabilities => ["weather", "weather-query", "forecast"];

        public WeatherAgent(
            IWeatherService weatherService,
            IMafAiAgentRegistry llmRegistry,
            ILogger<WeatherAgent> logger)
            : base(llmRegistry, logger)
        {
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
        }

        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            // 从参数中获取城市（必需）和日期（可选）
            string city = request.Parameters.TryGetValue("city", out var c)
                ? c?.ToString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(city))
            {
                // 缺少城市实体，触发澄清
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    NeedsClarification = true,
                    ClarificationQuestion = "请问您想查询哪个城市的天气？",
                    ClarificationOptions = ["北京", "上海", "广州", "成都", "杭州"],
                };
            }

            // 解析查询日期
            DateOnly queryDate = DateOnly.FromDateTime(DateTime.Today);
            if (request.Parameters.TryGetValue("date", out var d) && d != null)
            {
                var dateStr = d.ToString() ?? string.Empty;
                if (dateStr.Contains("明天") || dateStr.Equals("tomorrow", StringComparison.OrdinalIgnoreCase))
                    queryDate = queryDate.AddDays(1);
                else if (dateStr.Contains("后天"))
                    queryDate = queryDate.AddDays(2);
                // 默认：今天
            }

            Logger.LogInformation("WeatherAgent querying weather for {City} on {Date}", city, queryDate);

            try
            {
                var weather = await _weatherService.GetWeatherAsync(city, queryDate, ct);
                var response = FormatWeatherResponse(weather, queryDate);

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = response,
                    Data = weather,
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "WeatherAgent failed to get weather for {City}", city);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = ex.Message,
                    Result = $"抱歉，获取{city}天气信息失败，请稍后重试",
                };
            }
        }

        private static string FormatWeatherResponse(WeatherInfo weather, DateOnly queryDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var dayLabel = queryDate == today ? "今天"
                : queryDate == today.AddDays(1) ? "明天"
                : queryDate.ToString("M月d日");

            var aqiLevel = weather.AirQualityIndex switch
            {
                <= 50 => "优",
                <= 100 => "良好",
                <= 150 => "轻度污染",
                <= 200 => "中度污染",
                _ => "重度污染"
            };

            var lines = new List<string>
            {
                $"📍 {weather.City} {dayLabel}天气：{weather.Condition}",
                $"🌡️ 气温：{weather.MinTemperature}~{weather.MaxTemperature}°C（当前 {weather.Temperature}°C）",
                $"💨 {weather.WindDirection}{weather.WindLevel}级 | 💧 湿度 {weather.Humidity}% | 🌬️ 空气质量：{aqiLevel}（AQI {weather.AirQualityIndex}）",
                "",
                GetClothingAdvice(weather.Temperature),
                GetTravelAdvice(weather),
            };

            return string.Join("\n", lines.Where(l => l != null));
        }

        // 穿衣建议温度阈值（°C）
        private const int FreezingTemp = 5;    // 寒冷：穿厚羽绒服
        private const int CoolTemp = 12;       // 凉爽：穿外套
        private const int MildTemp = 18;       // 温和：穿薄外套
        private const int ComfortableTemp = 24; // 舒适：短袖即可

        private static string GetClothingAdvice(int temperature)
        {
            return temperature switch
            {
                <= FreezingTemp => "👔 穿衣建议：天气寒冷，建议穿厚羽绒服或大衣，注意保暖。",
                <= CoolTemp => "👔 穿衣建议：天气凉爽，建议穿外套或薄棉衣。",
                <= MildTemp => "👔 穿衣建议：温度适宜，建议穿薄外套或长袖衬衫。",
                <= ComfortableTemp => "👔 穿衣建议：天气舒适，穿短袖或薄衬衫即可。",
                _ => "👔 穿衣建议：天气炎热，建议穿轻薄透气的短袖，注意防晒。",
            };
        }

        private static string GetTravelAdvice(WeatherInfo weather)
        {
            var advice = new List<string>();

            if (weather.Condition.Contains("雨"))
                advice.Add("☂️ 出行建议：今天有雨，外出请携带雨伞。");
            else if (weather.Condition.Contains("雪"))
                advice.Add("❄️ 出行建议：今天有雪，路面可能湿滑，驾车请注意安全。");
            else if (weather.Condition.Contains("沙尘") || weather.Condition.Contains("雾"))
                advice.Add("😷 出行建议：能见度较低，外出建议佩戴口罩。");
            else if (weather.MaxTemperature >= 35)
                advice.Add("☀️ 出行建议：高温天气，户外活动请注意防暑，避免正午出行。");
            else
                advice.Add("✅ 出行建议：天气适宜出行，祝您出行愉快！");

            if (weather.AirQualityIndex > 150)
                advice.Add("😷 空气质量较差，建议减少户外活动并佩戴口罩。");

            return string.Join("\n", advice);
        }
    }
}
