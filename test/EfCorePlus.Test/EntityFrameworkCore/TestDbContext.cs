using EfCorePlus.Test.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCorePlus.Test.EntityFrameworkCore
{
    public class TestDbContext : DbContext, IEfCorePlusDbContext
    {
        public int? CurrentTenantId { get; set; }

        public DbSet<User> Users { get; set; }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureGlobalFilters(this);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity => entity.ToTable("Users"));
        }

        public string GetCompiledQueryCacheKey()
        {
            return CurrentTenantId?.ToString() ?? "Null";
        }
    }
}
