namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 时间因素
    /// </summary>
    public enum TimeFactor
    {
        /// <summary>可以延迟</summary>
        Deferred = 0,

        /// <summary>正常时间</summary>
        Normal = 1,

        /// <summary>紧急</summary>
        Urgent = 2,

        /// <summary>立即执行</summary>
        Immediate = 3
    }
}
