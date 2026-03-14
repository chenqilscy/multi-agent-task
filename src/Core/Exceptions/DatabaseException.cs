namespace CKY.MultiAgentFramework.Core.Exceptions
{
    /// <summary>
    /// 数据库异常（关系型数据库操作失败）
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// - 数据库连接失败（网络问题、数据库不可用）
    /// - SQL 查询执行失败（语法错误、权限不足）
    /// - 事务执行失败（死锁、超时）
    /// - 数据约束冲突（主键冲突、外键约束）
    /// - 数据类型转换失败
    ///
    /// 主要属性：
    /// - IsTransient: 是否为临时性错误（连接超时、死锁等可重试）
    ///
    /// 错误处理建议：
    /// - IsTransient = true: 启用重试机制（指数退避）
    /// - 约束冲突: 不重试，修复数据或逻辑
    /// - 连接失败: 检查数据库连接字符串和网络
    ///
    /// 临时性错误示例：
    /// - 连接超时（网络抖动）
    /// - 死锁（事务冲突）
    /// - 数据库繁忙（连接池满）
    ///
    /// 永久性错误示例：
    /// - 主键冲突（数据重复）
    /// - 外键约束（引用数据不存在）
    /// - 语法错误（SQL 编写错误）
    ///
    /// 示例：
    /// <code>
    /// try
    /// {
    ///     await _repository.SaveAsync(entity);
    /// }
    /// catch (DatabaseException ex) when (ex.IsTransient)
    /// {
    ///     // 临时性错误，启用重试
    ///     await Task.Delay(TimeSpan.FromSeconds(1));
    ///     await _repository.SaveAsync(entity);
    /// }
    /// catch (DatabaseException ex)
    /// {
    ///     // 永久性错误，记录日志并抛出
    ///     _logger.LogError(ex, "数据库操作永久性失败");
    ///     throw;
    /// }
    /// </code>
    /// </remarks>
    public class DatabaseException : MafException
    {
        /// <summary>
        /// 获取一个值，指示是否为临时性错误
        /// </summary>
        /// <value>
        /// 如果是临时性错误（如连接超时、死锁），则为 true；
        /// 如果是永久性错误（如约束冲突、语法错误），则为 false
        /// </value>
        public bool IsTransient { get; }

        /// <summary>
        /// 初始化 DatabaseException 类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="isTransient">是否为临时性错误</param>
        public DatabaseException(
            string message,
            bool isTransient = false)
            : base(MafErrorCode.DatabaseError, message, isRetryable: isTransient, component: "RelationalDatabase")
        {
            IsTransient = isTransient;
        }
    }
}
