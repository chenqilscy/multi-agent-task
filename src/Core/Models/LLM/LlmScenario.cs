namespace CKY.MultiAgentFramework.Core.Models.LLM
{
    /// <summary>
    /// LLM 使用场景枚举
    /// 定义不同的 LLM 应用场景，用于配置模型支持的功能
    /// </summary>
    public enum LlmScenario
    {
        /// <summary>聊天对话（通用文本生成）</summary>
        Chat = 1,

        /// <summary>文本嵌入（向量化，用于语义搜索）</summary>
        Embed = 2,

        /// <summary>意图识别（用户意图分析）</summary>
        Intent = 3,

        /// <summary>图像生成（文生图）</summary>
        Image = 4,

        /// <summary>视频生成（文生视频）</summary>
        Video = 5,

        /// <summary>代码生成</summary>
        Code = 6,

        /// <summary>摘要提取</summary>
        Summarization = 7,

        /// <summary>翻译</summary>
        Translation = 8
    }
}
