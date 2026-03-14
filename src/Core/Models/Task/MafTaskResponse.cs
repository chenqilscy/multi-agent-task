namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 任务响应
    /// </summary>
    public class MafTaskResponse
    {
        /// <summary>任务ID</summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>结果消息</summary>
        public string? Result { get; set; }

        /// <summary>结果数据</summary>
        public object? Data { get; set; }

        /// <summary>错误信息</summary>
        public string? Error { get; set; }

        /// <summary>子任务结果列表</summary>
        public List<SubTaskResult> SubTaskResults { get; set; } = new();

        /// <summary>完成时间</summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>是否需要用户澄清（缺少必要实体时为 true）</summary>
        public bool NeedsClarification { get; set; }

        /// <summary>澄清问题（NeedsClarification 为 true 时有值）</summary>
        public string? ClarificationQuestion { get; set; }

        /// <summary>澄清建议选项（可选，帮助用户快速选择）</summary>
        public List<string> ClarificationOptions { get; set; } = new();
    }
}
