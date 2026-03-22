using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 视频生成 Agent
    /// 负责根据文本描述生成视频内容
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 文生视频：根据文本描述生成短视频
    /// - 内容创作：为营销、教育生成视频内容
    /// - 故事叙述：将故事文本转化为视频
    /// - 产品展示：生成产品介绍视频
    /// </remarks>
    public class VideoAgent : MafBusinessAgentBase
    {
        public override string AgentId => "video-agent-001";
        public override string Name => "VideoAgent";
        public override string Description => "视频生成Agent，根据文本描述生成视频内容";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "text-to-video",
            "video-generation",
            "content-creation",
            "storytelling"
        };

        public VideoAgent(
            IMafAiAgentRegistry llmRegistry,
            ILogger<VideoAgent> logger)
            : base(llmRegistry, logger)
        {
        }

        /// <summary>
        /// 执行业务逻辑：视频生成
        /// </summary>
        public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var prompt = request.UserInput;
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return new MafTaskResponse
                    {
                        TaskId = request.TaskId,
                        Success = false,
                        Error = "视频描述不能为空"
                    };
                }

                Logger.LogInformation("[Video] 开始生成视频，描述: {Prompt}", prompt);

                // 提取参数
                var duration = GetParameter(request, "duration", 5); // 默认5秒
                var resolution = GetParameter(request, "resolution", "720p");
                var style = GetParameter(request, "style", "cinematic");

                // 构建完整的提示词
                var fullPrompt = BuildVideoPrompt(prompt, duration, resolution, style);

                // 调用 LLM 的 Video API
                var videoUrl = await CallLlmAsync(fullPrompt, LlmScenario.Video, null, ct);

                Logger.LogInformation("[Video] 视频生成完成");

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"视频已生成: {videoUrl}",
                    Data = new Dictionary<string, object>
                    {
                        ["video_url"] = videoUrl,
                        ["duration"] = duration,
                        ["resolution"] = resolution,
                        ["style"] = style,
                        ["prompt"] = prompt
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Video] 视频生成失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"视频生成失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 构建视频生成提示词
        /// </summary>
        private string BuildVideoPrompt(string prompt, int duration, string resolution, string style)
        {
            return $"Generate a {duration} second {style} style video: {prompt}. Resolution: {resolution}. High quality.";
        }
    }
}
