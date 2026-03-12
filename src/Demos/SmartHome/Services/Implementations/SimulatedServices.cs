using CKY.MultiAgentFramework.Demos.SmartHome.Services;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Services.Implementations
{
    /// <summary>
    /// 照明服务简单实现（模拟）
    /// 实际生产中应替换为与具体硬件平台（如小米、Tuya）的集成
    /// </summary>
    public class SimulatedLightingService : ILightingService
    {
        private readonly ILogger<SimulatedLightingService> _logger;
        private readonly Dictionary<string, (bool IsOn, int Brightness, string Color)> _state = new();

        public SimulatedLightingService(ILogger<SimulatedLightingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task TurnOnAsync(string room, CancellationToken ct = default)
        {
            _logger.LogInformation("Turning on light in {Room}", room);
            _state[room] = (true, 100, "#FFFFFF");
            return Task.CompletedTask;
        }

        public Task TurnOffAsync(string room, CancellationToken ct = default)
        {
            _logger.LogInformation("Turning off light in {Room}", room);
            _state[room] = (false, 0, "#000000");
            return Task.CompletedTask;
        }

        public Task SetBrightnessAsync(string room, int brightness, CancellationToken ct = default)
        {
            _logger.LogInformation("Setting brightness in {Room} to {Brightness}%", room, brightness);
            var current = _state.GetValueOrDefault(room, (true, 100, "#FFFFFF"));
            _state[room] = (current.Item1, brightness, current.Item3);
            return Task.CompletedTask;
        }

        public Task SetColorAsync(string room, string colorHex, CancellationToken ct = default)
        {
            _logger.LogInformation("Setting color in {Room} to {Color}", room, colorHex);
            var current = _state.GetValueOrDefault(room, (true, 100, "#FFFFFF"));
            _state[room] = (current.Item1, current.Item2, colorHex);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 气候控制服务简单实现（模拟）
    /// </summary>
    public class SimulatedClimateService : IClimateService
    {
        private readonly ILogger<SimulatedClimateService> _logger;
        private readonly Dictionary<string, (int Temperature, string Mode)> _state = new();

        public SimulatedClimateService(ILogger<SimulatedClimateService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SetTemperatureAsync(string room, int temperature, CancellationToken ct = default)
        {
            _logger.LogInformation("Setting temperature in {Room} to {Temperature}°C", room, temperature);
            var current = _state.GetValueOrDefault(room, (26, "auto"));
            _state[room] = (temperature, current.Item2);
            return Task.CompletedTask;
        }

        public Task SetModeAsync(string room, string mode, CancellationToken ct = default)
        {
            _logger.LogInformation("Setting climate mode in {Room} to {Mode}", room, mode);
            var current = _state.GetValueOrDefault(room, (26, "auto"));
            _state[room] = (current.Item1, mode);
            return Task.CompletedTask;
        }

        public Task<int> GetCurrentTemperatureAsync(string room, CancellationToken ct = default)
        {
            var temp = _state.GetValueOrDefault(room, (26, "auto")).Item1;
            return Task.FromResult(temp);
        }
    }
}
