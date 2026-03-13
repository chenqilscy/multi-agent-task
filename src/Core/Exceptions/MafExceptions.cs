namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// CKY.MAF错误码
    /// </summary>
    public enum MafErrorCode
    {
        // 通用错误 (1000-1099)
        Unknown = 1000,
        InvalidArgument = 1001,
        OperationCancelled = 1002,
        Timeout = 1003,

        // LLM服务错误 (2000-2099)
        LlmServiceError = 2000,
        LlmRateLimited = 2001,
        LlmContextTooLong = 2002,
        LlmAuthFailure = 2003,
        LlmModelUnavailable = 2004,

        // 缓存服务错误 (3000-3099)
        CacheServiceError = 3000,
        CacheConnectionFailed = 3001,
        CacheSerializationError = 3002,

        // 数据库错误 (4000-4099)
        DatabaseError = 4000,
        DatabaseConnectionFailed = 4001,
        DatabaseQueryFailed = 4002,
        DatabaseTransactionFailed = 4003,

        // 向量存储错误 (5000-5099)
        VectorStoreError = 5000,
        VectorStoreConnectionFailed = 5001,
        VectorSearchFailed = 5002,

        // 任务调度错误 (6000-6099)
        TaskSchedulingError = 6000,
        TaskDependencyCycleDetected = 6001,
        TaskExecutionTimeout = 6002,
        TaskMaxRetriesExceeded = 6003
    }

    /// <summary>
    /// CKY.MAF基础异常
    /// </summary>
    public abstract class MafException : Exception
    {
        public MafErrorCode ErrorCode { get; }
        public string Component { get; }
        public bool IsRetryable { get; }

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

    /// <summary>
    /// LLM服务异常
    /// </summary>
    public class LlmServiceException : MafException
    {
        public int? StatusCode { get; }
        public bool IsRateLimited { get; }

        public LlmServiceException(
            string message,
            int? statusCode = null,
            bool isRateLimited = false)
            : base(MafErrorCode.LlmServiceError, message, isRetryable: true, component: "LlmService")
        {
            StatusCode = statusCode;
            IsRateLimited = isRateLimited;
        }
    }

    /// <summary>
    /// LLM 弹性管道异常（重试失败后抛出）
    /// </summary>
    public class LlmResilienceException : MafException
    {
        public new Exception? InnerException { get; }

        public LlmResilienceException(
            string message,
            Exception? innerException = null)
            : base(MafErrorCode.LlmServiceError, message, isRetryable: false, component: "LlmResiliencePipeline")
        {
            InnerException = innerException;
        }
    }

    /// <summary>
    /// 缓存服务异常
    /// </summary>
    public class CacheServiceException : MafException
    {
        public CacheServiceException(string message)
            : base(MafErrorCode.CacheServiceError, message, isRetryable: true, component: "CacheStore")
        {
        }
    }

    /// <summary>
    /// 数据库异常
    /// </summary>
    public class DatabaseException : MafException
    {
        public bool IsTransient { get; }

        public DatabaseException(
            string message,
            bool isTransient = false)
            : base(MafErrorCode.DatabaseError, message, isRetryable: isTransient, component: "RelationalDatabase")
        {
            IsTransient = isTransient;
        }
    }

    /// <summary>
    /// 向量存储异常
    /// </summary>
    public class VectorStoreException : MafException
    {
        public VectorStoreException(string message)
            : base(MafErrorCode.VectorStoreError, message, isRetryable: true, component: "VectorStore")
        {
        }
    }

    /// <summary>
    /// 任务调度异常
    /// </summary>
    public class TaskSchedulingException : MafException
    {
        public string? TaskId { get; }

        public TaskSchedulingException(
            string? taskId,
            string message)
            : base(MafErrorCode.TaskSchedulingError, message, isRetryable: false, component: "TaskScheduler")
        {
            TaskId = taskId;
        }
    }
}
