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
}
