using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CKY.MultiAgentFramework.Infrastructure.Repository.Data
{
    /// <summary>
    /// EF Core 设计时工厂
    /// 用于在迁移创建时创建 DbContext 实例
    /// </summary>
    public class MafDbContextFactory : IDesignTimeDbContextFactory<MafDbContext>
    {
        public MafDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MafDbContext>();

            // 使用 SQLite 作为开发环境的默认数据库
            // 数据库文件将在项目根目录下创建
            optionsBuilder.UseSqlite("Data Source=maf_dev.db");

            return new MafDbContext(optionsBuilder.Options);
        }
    }
}
