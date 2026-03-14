namespace CKY.MultiAgentFramework.Infrastructure.Context
{
    /// <summary>
    /// 压缩统计信息
    /// </summary>
    public class CompressionStats
    {
        /// <summary>
        /// 总压缩次数
        /// </summary>
        public int TotalCompressions { get; set; }

        /// <summary>
        /// 平均压缩比例
        /// </summary>
        public double AverageCompressionRatio { get; set; }

        /// <summary>
        /// 最后一次压缩时间
        /// </summary>
        public DateTime LastCompressionTime { get; set; }
    }
}
