namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// CKY.MAF 框架基础异常类
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 所有 MAF 框架自定义异常都应继承此类
    /// - 提供统一的错误码、组件标识和重试标志
    /// - 支持结构化异常处理和日志记录
    ///
    /// 主要属性：
    /// - ErrorCode: 错误码，用于分类和识别错误类型
    /// - Component: 组件名称，标识发生错误的组件
    /// - IsRetryable: 是否可重试，指导错误恢复策略
    ///
    /// 使用场景：
    /// - LLM 服务调用失败（网络错误、超时、限流）
    /// - 缓存操作失败（连接失败、数据损坏）
    /// - 数据库操作失败（连接失败、查询错误）
    /// - 向量存储操作失败（索引失败、搜索超时）
    /// - 任务调度失败（依赖冲突、资源不足）
    ///
    /// 示例：
    /// <code>
    /// throw new LlmServiceException(
    ///     "API 调用失败",
    ///     statusCode: 500,
    ///     isRateLimited: false
    /// );
    /// </code>
    /// </remarks>
    public abstract class MafException : Exception
    {
        /// <summary>
        /// 获取错误码
        /// </summary>
        public MafErrorCode ErrorCode { get; }

        /// <summary>
        /// 获取发生错误的组件名称
        /// </summary>
        public string Component { get; }

        /// <summary>
        /// 获取一个值，指示异常是否可以重试
        /// </summary>
        /// <value>
        /// 如果异常可以重试（如网络超时、临时性错误），则为 true；
        /// 如果异常不可重试（如参数错误、配置错误），则为 false
        /// </value>
        public bool IsRetryable { get; }

        /// <summary>
        /// 初始化 MafException 类的新实例
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="message">错误消息</param>
        /// <param name="isRetryable">是否可重试</param>
        /// <param name="component">组件名称</param>
        protected MafException(
            MafErrorCode errorCode,
            string message,
            bool isRetryable = false,
            string component = "CKY.MAF")
            : base(message)
        {
            ErrorCode = errorCode;
            IsRetryable = isRetryable;
            Component = component;
        }
    }
}
