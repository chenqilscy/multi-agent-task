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
}
