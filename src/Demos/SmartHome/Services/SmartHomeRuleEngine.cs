using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Core.Resilience;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Services
{
    /// <summary>
    /// 智能家居规则引擎（Level 5 降级时替代 LLM）
    /// 仅使用关键词匹配和简单规则处理常见命令
    /// </summary>
    public class SmartHomeRuleEngine : IRuleEngine
    {
        private static readonly string[] SupportedKeywords =
        [
            "灯", "开灯", "关灯", "照明", "调亮", "调暗",
            "空调", "制冷", "制热", "温度",
            "音乐", "播放", "暂停",
            "锁门", "解锁", "摄像头", "外出模式",
        ];

        public bool CanHandle(string userInput)
        {
            return SupportedKeywords.Any(k =>
                userInput.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        public Task<MafTaskResponse> ProcessAsync(MafTaskRequest request, CancellationToken ct = default)
        {
            var input = request.UserInput;

            // 照明控制规则
            if (Contains(input, "开灯", "打开灯", "打开照明"))
            {
                return Ok(request, "💡 [规则引擎] 正在打开灯光（LLM 服务暂时不可用，已使用本地规则处理）");
            }
            if (Contains(input, "关灯", "关闭灯", "关闭照明"))
            {
                return Ok(request, "💡 [规则引擎] 正在关闭灯光");
            }
            if (Contains(input, "调暗"))
            {
                return Ok(request, "💡 [规则引擎] 正在调暗灯光至30%");
            }
            if (Contains(input, "调亮"))
            {
                return Ok(request, "💡 [规则引擎] 正在调亮灯光至100%");
            }

            // 空调控制规则
            if (Contains(input, "制冷", "开空调"))
            {
                return Ok(request, "❄️ [规则引擎] 空调已设置为制冷模式，目标温度26°C");
            }
            if (Contains(input, "制热"))
            {
                return Ok(request, "🔥 [规则引擎] 空调已设置为制热模式，目标温度22°C");
            }
            if (Contains(input, "关空调", "关闭空调"))
            {
                return Ok(request, "❄️ [规则引擎] 空调已关闭");
            }

            // 音乐控制规则
            if (Contains(input, "播放音乐", "放歌", "播放歌曲"))
            {
                return Ok(request, "🎵 [规则引擎] 正在播放默认歌单");
            }
            if (Contains(input, "暂停", "停止音乐"))
            {
                return Ok(request, "🎵 [规则引擎] 音乐已暂停");
            }

            // 安防规则
            if (Contains(input, "锁门", "上锁"))
            {
                return Ok(request, "🔒 [规则引擎] 大门已上锁");
            }
            if (Contains(input, "解锁", "开锁"))
            {
                return Ok(request, "🔓 [规则引擎] 大门已解锁");
            }
            if (Contains(input, "外出模式", "离家模式"))
            {
                return Ok(request, "🔐 [规则引擎] 外出安防模式已开启");
            }

            // 兜底
            return Task.FromResult(new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Result = "⚠️ [规则引擎] 当前LLM服务不可用，仅支持常见的灯光、空调、音乐和安防控制命令。",
            });
        }

        private static bool Contains(string input, params string[] keywords)
        {
            return keywords.Any(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static Task<MafTaskResponse> Ok(MafTaskRequest request, string result)
        {
            return Task.FromResult(new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = result,
            });
        }
    }
}
