namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// 降级级别（5级降级策略）
    /// </summary>
    public enum DegradationLevel
    {
        /// <summary>正常模式，所有功能可用</summary>
        Normal = 0,

        /// <summary>Level 1: 禁用非核心功能（个性化推荐等）</summary>
        Level1 = 1,

        /// <summary>Level 2: 禁用向量搜索，改用关键词搜索</summary>
        Level2 = 2,

        /// <summary>Level 3: 禁用L2分布式缓存，仅用L1内存缓存</summary>
        Level3 = 3,

        /// <summary>Level 4: 使用简化LLM模型（GLM-4-Air代替GLM-4-Plus）</summary>
        Level4 = 4,

        /// <summary>Level 5: 完全禁用LLM，仅用规则引擎处理</summary>
        Level5 = 5,
    }
}
