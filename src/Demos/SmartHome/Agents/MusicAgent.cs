using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Agents
{
    /// <summary>
    /// 音乐播放Agent
    /// 负责处理音乐播放、暂停、切歌等控制命令
    /// </summary>
    public class MusicAgent : MafBusinessAgentBase
    {
        public override string AgentId => "music-agent-001";
        public override string Name => "MusicAgent";
        public override string Description => "智能音乐播放Agent，支持播放、暂停、切歌等功能";
        public override IReadOnlyList<string> Capabilities => ["music", "audio", "media-control"];

        public MusicAgent(
            IMafAiAgentRegistry llmRegistry,
            ILogger<MusicAgent> logger)
            : base(llmRegistry, logger)
        {
        }

        public override Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            var userInput = request.UserInput;

            if (userInput.Contains("播放") || userInput.Contains("放音乐"))
            {
                return Task.FromResult(new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = "正在播放音乐"
                });
            }
            else if (userInput.Contains("暂停") || userInput.Contains("停止"))
            {
                return Task.FromResult(new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = "音乐已暂停"
                });
            }
            else if (userInput.Contains("下一首") || userInput.Contains("换歌"))
            {
                return Task.FromResult(new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = "已切换到下一首歌曲"
                });
            }

            return Task.FromResult(new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Error = "无法识别的音乐控制命令"
            });
        }
    }
}
