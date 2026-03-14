namespace CKY.MultiAgentFramework.Core.Constants
{
    /// <summary>
    /// 持久化相关常量
    /// </summary>
    public static class PersistenceConstants
    {
        /// <summary>
        /// 数据库字段长度限制
        /// </summary>
        public static class FieldLengths
        {
            /// <summary>PlanId 最大长度</summary>
            public const int PlanIdMaxLength = 100;

            /// <summary>TaskId 最大长度</summary>
            public const int TaskIdMaxLength = 100;

            /// <summary>错误消息最大长度</summary>
            public const int ErrorMessageMaxLength = 1000;

            /// <summary>消息最大长度</summary>
            public const int MessageMaxLength = 4000;

            /// <summary>JSON 数据最大长度</summary>
            public const int DataJsonMaxLength = 8000;
        }

        /// <summary>
        /// 任务优先级阈值
        /// </summary>
        public static class PriorityThresholds
        {
            /// <summary>高优先级阈值</summary>
            public const int HighPriorityThreshold = 50;

            /// <summary>中优先级下限</summary>
            public const int MediumPriorityThreshold = 30;

            /// <summary>低优先级上限</summary>
            public const int LowPriorityThreshold = 30;
        }

        /// <summary>
        /// 数据库索引名称
        /// </summary>
        public static class IndexNames
        {
            /// <summary>调度计划状态+创建时间复合索引</summary>
            public const string SchedulePlans_Status_CreatedAt = "IX_SchedulePlans_Status_CreatedAt";

            /// <summary>调度计划 PlanId+状态复合索引</summary>
            public const string SchedulePlans_PlanId_Status = "IX_SchedulePlans_PlanId_Status";

            /// <summary>执行计划状态+创建时间复合索引</summary>
            public const string ExecutionPlans_Status_CreatedAt = "IX_ExecutionPlans_Status_CreatedAt";

            /// <summary>执行计划 PlanId+状态复合索引</summary>
            public const string ExecutionPlans_PlanId_Status = "IX_ExecutionPlans_PlanId_Status";

            /// <summary>任务执行结果 PlanId+成功标志复合索引</summary>
            public const string TaskExecutionResults_PlanId_Success = "IX_TaskExecutionResults_PlanId_Success";

            /// <summary>任务执行结果 PlanId+创建时间复合索引</summary>
            public const string TaskExecutionResults_PlanId_CreatedAt = "IX_TaskExecutionResults_PlanId_CreatedAt";
        }

        /// <summary>
        /// 默认值常量
        /// </summary>
        public static class Defaults
        {
            /// <summary>默认优先级分数</summary>
            public const int DefaultPriorityScore = 50;

            /// <summary>默认获取数量</summary>
            public const int DefaultFetchCount = 10;

            /// <summary>默认最大执行时间（分钟）</summary>
            public const int DefaultMaxExecutionTimeMinutes = 5;

            /// <summary>默认最大等待时间（分钟）</summary>
            public const int DefaultMaxWaitTimeMinutes = 5;
        }

        /// <summary>
        /// 持久化策略名称
        /// </summary>
        public static class Strategies
        {
            /// <summary>从持久化恢复的策略名称</summary>
            public const string RestoreFromPersistence = "Restored from persistence";

            /// <summary>序列化版本号</summary>
            public const string SerializationVersion = "1.0";
        }
    }

    /// <summary>
    /// 错误消息常量
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>任务未找到</summary>
        public const string TaskNotFound = "任务未找到";

        /// <summary>计划未找到</summary>
        public const string PlanNotFound = "执行计划未找到";

        /// <summary>无效的任务状态</summary>
        public const string InvalidTaskStatus = "无效的任务状态";

        /// <summary>持久化失败</summary>
        public const string PersistenceFailed = "持久化操作失败";

        /// <summary>反序列化失败</summary>
        public const string DeserializationFailed = "数据反序列化失败";
    }
}
