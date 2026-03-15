namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// 降级管理器接口
    /// 根据系统健康状况动态调整降级级别
    /// </summary>
    public interface IDegradationManager
    {
        /// <summary>当前降级级别</summary>
        DegradationLevel CurrentLevel { get; }

        /// <summary>设置降级级别</summary>
        void SetLevel(DegradationLevel level);

        /// <summary>根据当前降级级别判断指定功能是否启用</summary>
        bool IsFeatureEnabled(string feature);
    }
}
