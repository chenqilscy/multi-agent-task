namespace CKY.MultiAgentFramework.Demos.SmartHome.Models
{
    /// <summary>
    /// 智能家居响应模型
    /// </summary>
    public class SmartHomeResponse
    {
        public bool Success { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Error { get; set; }

        /// <summary>是否需要用户澄清（缺少必要实体时为 true）</summary>
        public bool NeedsClarification { get; set; }

        /// <summary>澄清问题</summary>
        public string? ClarificationQuestion { get; set; }

        /// <summary>澄清建议选项</summary>
        public List<string> ClarificationOptions { get; set; } = new();
    }

    /// <summary>
    /// 设备状态模型
    /// </summary>
    public class DeviceStatus
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public bool IsOn { get; set; }
        public object? Value { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// 照明设备状态
    /// </summary>
    public class LightingDeviceStatus : DeviceStatus
    {
        public int Brightness { get; set; } // 0-100
        public string? Color { get; set; }
    }

    /// <summary>
    /// 气候设备状态
    /// </summary>
    public class ClimateDeviceStatus : DeviceStatus
    {
        public double Temperature { get; set; }
        public string Mode { get; set; } = "auto"; // auto, cooling, heating, off
        public int FanSpeed { get; set; } // 0-3
    }

    /// <summary>
    /// 音乐设备状态
    /// </summary>
    public class MusicDeviceStatus : DeviceStatus
    {
        public string? CurrentSong { get; set; }
        public int Volume { get; set; } = 50;
        public bool IsPlaying { get; set; }
    }
}
