using EfCorePlus.Test.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCorePlus.Test.EntityFrameworkCore
{
    public class TestDbContext : DbContext, IEfCorePlusDbContext
    {
        public int? CurrentTenantId { get; set; }

        public DbSet<SoftDeleteTestData> SoftDeleteTestDatas { get; set; }

        public DbSet<TanentTestData> TanentTestDatas { get; set; }

        public DbSet<ActiveTestData> ActiveTestDatas { get; set; }

        public DbSet<Blog> Blogs { get; set; }

        public DbSet<Post> Posts { get; set; }

        public DbSet<User> Users { get; set; }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ConfigureGlobalFilters(this);
        }

        public string GetCompiledQueryCacheKey()
        {
            return CurrentTenantId?.ToString() ?? "Null";
        }
    }
}
