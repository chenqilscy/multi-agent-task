namespace CKY.MultiAgentFramework.Demos.SmartHome.Services
{
    /// <summary>
    /// 照明服务接口
    /// </summary>
    public interface ILightingService
    {
        /// <summary>打开指定房间的灯</summary>
        Task TurnOnAsync(string room, CancellationToken ct = default);

        /// <summary>关闭指定房间的灯</summary>
        Task TurnOffAsync(string room, CancellationToken ct = default);

        /// <summary>设置灯光亮度（0-100）</summary>
        Task SetBrightnessAsync(string room, int brightness, CancellationToken ct = default);

        /// <summary>设置灯光颜色（十六进制颜色代码）</summary>
        Task SetColorAsync(string room, string colorHex, CancellationToken ct = default);
    }

    /// <summary>
    /// 气候控制服务接口
    /// </summary>
    public interface IClimateService
    {
        /// <summary>设置温度</summary>
        Task SetTemperatureAsync(string room, int temperature, CancellationToken ct = default);

        /// <summary>设置模式（cooling/heating/fan/auto）</summary>
        Task SetModeAsync(string room, string mode, CancellationToken ct = default);

        /// <summary>获取当前温度</summary>
        Task<int> GetCurrentTemperatureAsync(string room, CancellationToken ct = default);
    }

    /// <summary>
    /// 天气信息
    /// </summary>
    public class WeatherInfo
    {
        /// <summary>城市</summary>
        public string City { get; set; } = string.Empty;

        /// <summary>查询日期</summary>
        public DateOnly Date { get; set; }

        /// <summary>天气状况（晴/多云/阴/小雨/大雨/雪）</summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>当前温度（℃）</summary>
        public int Temperature { get; set; }

        /// <summary>最低温度（℃）</summary>
        public int MinTemperature { get; set; }

        /// <summary>最高温度（℃）</summary>
        public int MaxTemperature { get; set; }

        /// <summary>风向</summary>
        public string WindDirection { get; set; } = string.Empty;

        /// <summary>风力等级</summary>
        public int WindLevel { get; set; }

        /// <summary>空气质量指数（AQI）</summary>
        public int AirQualityIndex { get; set; }

        /// <summary>湿度（%）</summary>
        public int Humidity { get; set; }
    }

    /// <summary>
    /// 天气服务接口（案例1：天气查询）
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// 获取指定城市、指定日期的天气信息
        /// </summary>
        Task<WeatherInfo> GetWeatherAsync(string city, DateOnly date, CancellationToken ct = default);

        /// <summary>
        /// 获取未来N天天气预报
        /// </summary>
        Task<List<WeatherInfo>> GetForecastAsync(string city, int days, CancellationToken ct = default);
    }

    /// <summary>
    /// 传感器温度记录
    /// </summary>
    public class TemperatureRecord
    {
        /// <summary>记录时间</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>房间名</summary>
        public string Room { get; set; } = string.Empty;

        /// <summary>温度（℃）</summary>
        public double Temperature { get; set; }

        /// <summary>湿度（%）</summary>
        public double Humidity { get; set; }
    }

    /// <summary>
    /// 传感器数据服务接口（案例2：历史温度查询）
    /// </summary>
    public interface ISensorDataService
    {
        /// <summary>
        /// 获取指定房间过去N天的温度历史数据
        /// </summary>
        Task<List<TemperatureRecord>> GetTemperatureHistoryAsync(
            string room, int days, CancellationToken ct = default);

        /// <summary>
        /// 获取当前温度读数
        /// </summary>
        Task<TemperatureRecord> GetCurrentTemperatureAsync(
            string room, CancellationToken ct = default);
    }
}
