using CKY.MultiAgentFramework.Core.Enums;

namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 任务请求（用户发起的任务执行请求）
    /// </summary>
    /// <remarks>
    /// 设计目的：
    /// - 封装用户的任务请求信息
    /// - 支持显式参数和隐式上下文
    /// - 提供任务调度的基础信息
    /// - 支持多轮对话的上下文传递
    ///
    /// 主要属性：
    /// - UserInput: 用户的原始输入（自然语言描述）
    /// - Parameters: 显式参数（用户明确指定的参数）
    /// - Context: 隐式上下文（从会话历史、配置等推断的上下文）
    /// - Priority: 任务优先级
    ///
    /// 参数与上下文的区别：
    /// - Parameters: 显式参数，用户明确指定的值（如 "生成一个 C# 类"）
    /// - Context: 隐式上下文，系统自动推断的值（如用户偏好、历史记录）
    ///
    /// 使用场景：
    /// <code>
    /// // 用户请求：生成一个用户认证 API
    /// var request = new MafTaskRequest
    /// {
    ///     UserInput = "生成一个基于 JWT 的用户认证 API",
    ///     ConversationId = "conv-123",
    ///     UserId = "user-456",
    ///     Priority = TaskPriority.High,
    ///     Parameters = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["Language"] = "C#",
    ///         ["Framework"] = "ASP.NET Core"
    ///     },
    ///     Context = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["ProjectType"] = "WebAPI",
    ///         ["PreviousCodeStyle"] = "Modern"
    ///     }
    /// };
    /// </code>
    ///
    /// 任务处理流程：
    /// 1. 接收请求（MafTaskRequest）
    /// 2. 意图识别（分析用户意图）
    /// 3. 任务分解（DecomposedTask 列表）
    /// 4. 任务调度（按优先级和依赖）
    /// 5. 任务执行（Agent 执行）
    /// 6. 结果返回（MafTaskResponse）
    /// </remarks>
    public class MafTaskRequest
    {
        /// <summary>
        /// 获取或设置任务唯一标识
        /// </summary>
        public string TaskId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 获取或设置用户原始输入（自然语言描述）
        /// </summary>
        public string UserInput { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置对话 ID（用于多轮对话上下文）
        /// </summary>
        public string ConversationId { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置用户 ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置显式参数（用户明确指定的参数）
        /// </summary>
        /// <remarks>
        /// 示例：["Language"] = "C#", ["Framework"] = "ASP.NET Core"
        /// </remarks>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 获取或设置隐式上下文（从会话历史、配置等推断的上下文）
        /// </summary>
        /// <remarks>
        /// 示例：["ProjectType"] = "WebAPI", ["PreviousCodeStyle"] = "Modern"
        /// </remarks>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// 获取或设置任务优先级
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    }
}
