using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Services.Implementations
{
    /// <summary>
    /// 故障注入接口 - 用于测试和演示故障场景
    /// </summary>
    public interface IFaultInjectable
    {
        /// <summary>注入故障（设置为 true 后，后续调用将抛出异常）</summary>
        void InjectFault(string faultType, string? message = null);

        /// <summary>清除所有注入的故障</summary>
        void ClearFaults();
    }

    /// <summary>
    /// 故障注入基类，封装通用故障注入逻辑
    /// </summary>
    public abstract class FaultInjectableServiceBase : IFaultInjectable
    {
        private readonly Dictionary<string, string> _faults = new(StringComparer.OrdinalIgnoreCase);

        public void InjectFault(string faultType, string? message = null)
        {
            _faults[faultType] = message ?? $"Injected fault: {faultType}";
        }

        public void ClearFaults()
        {
            _faults.Clear();
        }

        /// <summary>如果有对应类型的故障注入，则抛出异常</summary>
        protected void ThrowIfFaultInjected(string faultType)
        {
            if (_faults.TryGetValue(faultType, out var message))
                throw new InvalidOperationException(message);
        }

        /// <summary>如果有任何故障注入，则抛出异常</summary>
        protected void ThrowIfAnyFaultInjected()
        {
            if (_faults.Count > 0)
            {
                var first = _faults.First();
                throw new InvalidOperationException(first.Value);
            }
        }

        /// <summary>检查指定类型的故障是否已注入</summary>
        protected bool HasFault(string faultType) => _faults.ContainsKey(faultType);

        /// <summary>模拟超时延迟</summary>
        protected async Task SimulateTimeoutIfInjectedAsync(string faultType, CancellationToken ct)
        {
            if (_faults.TryGetValue(faultType, out _))
                throw new TimeoutException($"Service timeout: {faultType}");
        }
    }

    /// <summary>
    /// 照明服务简单实现（模拟）
    /// 支持故障注入：device_offline, device_failure
    /// </summary>
    public class SimulatedLightingService : FaultInjectableServiceBase, ILightingService
    {
        private readonly ILogger<SimulatedLightingService> _logger;
        private readonly Dictionary<string, (bool IsOn, int Brightness, string Color)> _state = new();

        public SimulatedLightingService(ILogger<SimulatedLightingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task TurnOnAsync(string room, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            ThrowIfFaultInjected("device_failure");
            _logger.LogInformation("Turning on light in {Room}", room);
            _state[room] = (true, 100, "#FFFFFF");
            return Task.CompletedTask;
        }

        public Task TurnOffAsync(string room, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            ThrowIfFaultInjected("device_failure");
            _logger.LogInformation("Turning off light in {Room}", room);
            _state[room] = (false, 0, "#000000");
            return Task.CompletedTask;
        }

        public Task SetBrightnessAsync(string room, int brightness, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            _logger.LogInformation("Setting brightness in {Room} to {Brightness}%", room, brightness);
            var current = _state.GetValueOrDefault(room, (true, 100, "#FFFFFF"));
            _state[room] = (current.Item1, brightness, current.Item3);
            return Task.CompletedTask;
        }

        public Task SetColorAsync(string room, string colorHex, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            _logger.LogInformation("Setting color in {Room} to {Color}", room, colorHex);
            var current = _state.GetValueOrDefault(room, (true, 100, "#FFFFFF"));
            _state[room] = (current.Item1, current.Item2, colorHex);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 气候控制服务简单实现（模拟）
    /// 支持故障注入：device_offline, device_failure
    /// </summary>
    public class SimulatedClimateService : FaultInjectableServiceBase, IClimateService
    {
        private readonly ILogger<SimulatedClimateService> _logger;
        private readonly Dictionary<string, (int Temperature, string Mode)> _state = new();
        private bool _gasValveOpen = true;

        public SimulatedClimateService(ILogger<SimulatedClimateService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SetTemperatureAsync(string room, int temperature, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            _logger.LogInformation("Setting temperature in {Room} to {Temperature}°C", room, temperature);
            var current = _state.GetValueOrDefault(room, (26, "auto"));
            _state[room] = (temperature, current.Item2);
            return Task.CompletedTask;
        }

        public Task SetModeAsync(string room, string mode, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            _logger.LogInformation("Setting climate mode in {Room} to {Mode}", room, mode);
            var current = _state.GetValueOrDefault(room, (26, "auto"));
            _state[room] = (current.Item1, mode);
            return Task.CompletedTask;
        }

        public Task<int> GetCurrentTemperatureAsync(string room, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            var temp = _state.GetValueOrDefault(room, (26, "auto")).Item1;
            return Task.FromResult(temp);
        }

        public Task<bool> CloseGasValveAsync(CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_failure");
            _logger.LogWarning("EMERGENCY: Closing gas valve!");
            _gasValveOpen = false;
            return Task.FromResult(true);
        }

        public Task<bool> OpenGasValveAsync(CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_failure");
            _logger.LogInformation("Opening gas valve");
            _gasValveOpen = true;
            return Task.FromResult(true);
        }

        public Task<bool> IsGasValveOpenAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_gasValveOpen);
        }
    }

    /// <summary>
    /// 天气服务模拟实现（案例1：天气查询）
    /// 支持故障注入：service_unavailable, timeout
    /// </summary>
    public class SimulatedWeatherService : FaultInjectableServiceBase, IWeatherService
    {
        private static readonly Random _rng = new(42);

        private static readonly Dictionary<string, string[]> _cityConditions = new()
        {
            ["北京"] = ["晴", "多云", "阴", "小雨", "沙尘"],
            ["上海"] = ["晴", "多云", "小雨", "中雨", "雾"],
            ["广州"] = ["晴", "多云", "雷阵雨", "小雨", "台风"],
            ["成都"] = ["多云", "阴", "小雨", "雾", "晴"],
            ["杭州"] = ["晴", "多云", "小雨", "中雨", "阴"],
        };

        public Task<WeatherInfo> GetWeatherAsync(string city, DateOnly date, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("service_unavailable");
            ThrowIfFaultInjected("timeout");
            var conditions = _cityConditions.GetValueOrDefault(city, ["晴", "多云", "阴"]);
            var condition = conditions[_rng.Next(conditions.Length)];
            var baseTemp = city switch
            {
                "北京" => 12, "上海" => 16, "广州" => 24, "成都" => 14, "杭州" => 15, _ => 18
            };

            var info = new WeatherInfo
            {
                City = city,
                Date = date,
                Condition = condition,
                Temperature = baseTemp + _rng.Next(-3, 4),
                MinTemperature = baseTemp - 5 + _rng.Next(-2, 2),
                MaxTemperature = baseTemp + 6 + _rng.Next(-2, 2),
                WindDirection = new[] { "东风", "南风", "西风", "北风", "东北风" }[_rng.Next(5)],
                WindLevel = _rng.Next(1, 5),
                AirQualityIndex = _rng.Next(30, 150),
                Humidity = _rng.Next(40, 85),
            };

            return Task.FromResult(info);
        }

        public async Task<List<WeatherInfo>> GetForecastAsync(
            string city, int days, CancellationToken ct = default)
        {
            var result = new List<WeatherInfo>();
            var today = DateOnly.FromDateTime(DateTime.Today);
            for (int i = 0; i < days; i++)
                result.Add(await GetWeatherAsync(city, today.AddDays(i), ct));
            return result;
        }
    }

    /// <summary>
    /// 传感器数据服务模拟实现（案例2：历史温度查询）
    /// 生产环境应替换为真实传感器数据库查询
    /// </summary>
    public class SimulatedSensorDataService : ISensorDataService
    {
        private static readonly Random _rng = new(99);

        // 模拟各房间基准温度
        private static readonly Dictionary<string, double> _baseTemps = new()
        {
            ["客厅"] = 22.0, ["卧室"] = 20.5, ["厨房"] = 24.0,
            ["书房"] = 21.0, ["浴室"] = 25.0, ["餐厅"] = 22.5,
        };

        public Task<List<TemperatureRecord>> GetTemperatureHistoryAsync(
            string room, int days, CancellationToken ct = default)
        {
            var baseTemp = _baseTemps.GetValueOrDefault(room, 22.0);
            var records = new List<TemperatureRecord>();
            var now = DateTime.Now;

            // 每天生成4个采样点（6时、12时、18时、24时）
            for (int d = days - 1; d >= 0; d--)
            {
                for (int h = 0; h < 24; h += 6)
                {
                    // 模拟日内温度变化（早晚低、中午高）
                    var hourFactor = h == 12 ? 2.0 : h == 18 ? 1.0 : h == 6 ? -1.0 : 0.0;
                    var dayVariation = (_rng.NextDouble() - 0.5) * 3.0;
                    var temp = baseTemp + hourFactor + dayVariation;

                    records.Add(new TemperatureRecord
                    {
                        Timestamp = now.AddDays(-d).Date.AddHours(h),
                        Room = room,
                        Temperature = Math.Round(temp, 1),
                        Humidity = Math.Round(45.0 + _rng.NextDouble() * 30.0, 1),
                    });
                }
            }

            return Task.FromResult(records);
        }

        public Task<TemperatureRecord> GetCurrentTemperatureAsync(
            string room, CancellationToken ct = default)
        {
            var baseTemp = _baseTemps.GetValueOrDefault(room, 22.0);
            var record = new TemperatureRecord
            {
                Timestamp = DateTime.Now,
                Room = room,
                Temperature = Math.Round(baseTemp + (_rng.NextDouble() - 0.5) * 2.0, 1),
                Humidity = Math.Round(50.0 + _rng.NextDouble() * 20.0, 1),
            };
            return Task.FromResult(record);
        }
    }

    /// <summary>
    /// 安防服务模拟实现
    /// 支持故障注入：device_offline, device_failure
    /// </summary>
    public class SimulatedSecurityService : FaultInjectableServiceBase, ISecurityService
    {
        private readonly ILogger<SimulatedSecurityService> _logger;
        private readonly Dictionary<string, DoorLockStatus> _doorLocks = new();
        private readonly Dictionary<string, CameraStatus> _cameras = new();
        private readonly List<SecurityAlert> _alerts = new();
#pragma warning disable CS0414 // 模拟状态字段：当前仅写入，未来可扩展查询
        private bool _awayMode;
        private bool _presenceSimulation;
#pragma warning restore CS0414

        public SimulatedSecurityService(ILogger<SimulatedSecurityService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化默认门锁
            _doorLocks["大门"] = new DoorLockStatus { Location = "大门", IsLocked = true, LastChanged = DateTime.Now };

            // 初始化默认摄像头
            _cameras["门口"] = new CameraStatus { Location = "门口", IsActive = false, HasMotionDetection = true };
            _cameras["客厅"] = new CameraStatus { Location = "客厅", IsActive = false, HasMotionDetection = true };
            _cameras["车库"] = new CameraStatus { Location = "车库", IsActive = false, HasMotionDetection = false };
        }

        public Task<bool> LockDoorAsync(string location = "大门", CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            _logger.LogInformation("Locking door at {Location}", location);
            _doorLocks[location] = new DoorLockStatus { Location = location, IsLocked = true, LastChanged = DateTime.Now };
            return Task.FromResult(true);
        }

        public Task<bool> UnlockDoorAsync(string location = "大门", CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            _logger.LogInformation("Unlocking door at {Location}", location);
            _doorLocks[location] = new DoorLockStatus { Location = location, IsLocked = false, LastChanged = DateTime.Now };
            return Task.FromResult(true);
        }

        public Task<DoorLockStatus> GetDoorLockStatusAsync(string location = "大门", CancellationToken ct = default)
        {
            var status = _doorLocks.GetValueOrDefault(location,
                new DoorLockStatus { Location = location, IsLocked = false, LastChanged = DateTime.Now });
            return Task.FromResult(status);
        }

        public Task<bool> SetCameraActiveAsync(string location, bool active, CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_offline");
            _logger.LogInformation("Setting camera at {Location} to {Active}", location, active ? "active" : "inactive");
            if (_cameras.TryGetValue(location, out var camera))
            {
                camera.IsActive = active;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<List<CameraStatus>> GetCameraStatusListAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_cameras.Values.ToList());
        }

        public Task<bool> EnableAwayModeAsync(CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_failure");
            _logger.LogInformation("Enabling away mode: locking doors, activating cameras");
            _awayMode = true;
            foreach (var door in _doorLocks.Values)
                door.IsLocked = true;
            foreach (var camera in _cameras.Values)
                camera.IsActive = true;
            return Task.FromResult(true);
        }

        public Task<bool> DisableAwayModeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Disabling away mode");
            _awayMode = false;
            foreach (var camera in _cameras.Values)
                camera.IsActive = false;
            return Task.FromResult(true);
        }

        public Task<List<SecurityAlert>> GetRecentAlertsAsync(int count = 10, CancellationToken ct = default)
        {
            var recent = _alerts.OrderByDescending(a => a.Timestamp).Take(count).ToList();
            return Task.FromResult(recent);
        }

        public Task<bool> EnablePresenceSimulationAsync(CancellationToken ct = default)
        {
            ThrowIfFaultInjected("device_failure");
            _logger.LogInformation("Enabling presence simulation (random light switching)");
            _presenceSimulation = true;
            return Task.FromResult(true);
        }

        public Task<bool> DisablePresenceSimulationAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Disabling presence simulation");
            _presenceSimulation = false;
            return Task.FromResult(true);
        }
    }
}
