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

        /// <summary>关闭燃气阀门（紧急安全操作）</summary>
        Task<bool> CloseGasValveAsync(CancellationToken ct = default);

        /// <summary>打开燃气阀门</summary>
        Task<bool> OpenGasValveAsync(CancellationToken ct = default);

        /// <summary>获取燃气阀门状态</summary>
        Task<bool> IsGasValveOpenAsync(CancellationToken ct = default);
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

    // ============================
    // 安防相关模型和接口
    // ============================

    /// <summary>门锁状态</summary>
    public class DoorLockStatus
    {
        public string Location { get; set; } = "大门";
        public bool IsLocked { get; set; }
        public DateTime LastChanged { get; set; }
    }

    /// <summary>摄像头状态</summary>
    public class CameraStatus
    {
        public string Location { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool HasMotionDetection { get; set; }
    }

    /// <summary>安防警报</summary>
    public class SecurityAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // intrusion, smoke, gas_leak, water_leak
        public string Location { get; set; } = string.Empty;
        public string Severity { get; set; } = "medium"; // low, medium, high, critical
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 安防服务接口
    /// 管理门锁、摄像头和安全检测
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>锁定门锁</summary>
        Task<bool> LockDoorAsync(string location = "大门", CancellationToken ct = default);

        /// <summary>解锁门锁</summary>
        Task<bool> UnlockDoorAsync(string location = "大门", CancellationToken ct = default);

        /// <summary>获取门锁状态</summary>
        Task<DoorLockStatus> GetDoorLockStatusAsync(string location = "大门", CancellationToken ct = default);

        /// <summary>启用/禁用摄像头</summary>
        Task<bool> SetCameraActiveAsync(string location, bool active, CancellationToken ct = default);

        /// <summary>获取所有摄像头状态</summary>
        Task<List<CameraStatus>> GetCameraStatusListAsync(CancellationToken ct = default);

        /// <summary>启动外出安防模式</summary>
        Task<bool> EnableAwayModeAsync(CancellationToken ct = default);

        /// <summary>关闭外出安防模式</summary>
        Task<bool> DisableAwayModeAsync(CancellationToken ct = default);

        /// <summary>获取最近的安防警报</summary>
        Task<List<SecurityAlert>> GetRecentAlertsAsync(int count = 10, CancellationToken ct = default);

        /// <summary>模拟有人在家（随机开关灯）</summary>
        Task<bool> EnablePresenceSimulationAsync(CancellationToken ct = default);

        /// <summary>关闭模拟有人在家</summary>
        Task<bool> DisablePresenceSimulationAsync(CancellationToken ct = default);
    }
}
