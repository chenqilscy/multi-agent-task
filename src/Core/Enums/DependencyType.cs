namespace CKY.MultiAgentFramework.Core.Enums
{
    /// <summary>
    /// 任务依赖类型枚举
    /// </summary>
    public enum DependencyType
    {
        /// <summary>必须完成（无论成功失败）</summary>
        MustComplete,
        /// <summary>必须成功</summary>
        MustSucceed,
        /// <summary>必须已开始</summary>
        MustStart,
        /// <summary>数据依赖（需要输出数据）</summary>
        DataDependency,
        /// <summary>软依赖（可选，优先级继承）</summary>
        SoftDependency
    }
}
