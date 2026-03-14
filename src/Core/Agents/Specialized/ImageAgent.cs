using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents.Specialized
{
    /// <summary>
    /// 图片生成 Agent
    /// 负责根据文本描述生成图片
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 文生图：根据文本描述生成图片
    /// - 图片编辑：修改现有图片的风格、内容等
    /// - 创意设计：生成海报、Logo、插图等
    /// - 内容创作：为文章、博客生成配图
    /// </remarks>
    public class ImageAgent : MafAgentBase
    {
        public override string AgentId => "image-agent-001";
        public override string Name => "ImageAgent";
        public override string Description => "图片生成Agent，根据文本描述生成或编辑图片";
        public override IReadOnlyList<string> Capabilities => new[]
        {
            "text-to-image",
            "image-generation",
            "creative-design",
            "illustration"
        };

        public ImageAgent(
            ILlmAgentRegistry llmRegistry,
            ILogger<ImageAgent> logger)
            : base(llmRegistry, logger)
        {
        }

        /// <summary>
        /// 执行业务逻辑：图片生成
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
                        Error = "图片描述不能为空"
                    };
                }

                Logger.LogInformation("[Image] 开始生成图片，描述: {Prompt}", prompt);

                // 提取参数
                var width = GetParameter(request, "width", 1024);
                var height = GetParameter(request, "height", 1024);
                var style = GetParameter(request, "style", "realistic");

                // 构建完整的提示词
                var fullPrompt = BuildImagePrompt(prompt, width, height, style);

                // 调用 LLM 的 Image API
                var imageUrl = await CallLlmAsync(fullPrompt, LlmScenario.Image, null, ct);

                Logger.LogInformation("[Image] 图片生成完成");

                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = true,
                    Result = $"图片已生成: {imageUrl}",
                    Data = new Dictionary<string, object>
                    {
                        ["image_url"] = imageUrl,
                        ["width"] = width,
                        ["height"] = height,
                        ["style"] = style,
                        ["prompt"] = prompt
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Image] 图片生成失败");
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = $"图片生成失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 构建图片生成提示词
        /// </summary>
        private string BuildImagePrompt(string prompt, int width, int height, string style)
        {
            return $"Generate a {style} style image: {prompt}. Size: {width}x{height}. High quality, detailed.";
        }

        /// <summary>
        /// 从请求中提取参数
        /// </summary>
        private T GetParameter<T>(MafTaskRequest request, string key, T defaultValue)
        {
            if (request.Parameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
