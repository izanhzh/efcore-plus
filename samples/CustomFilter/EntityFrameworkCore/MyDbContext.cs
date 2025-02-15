using CustomFilter.EntityFrameworkCore.Entities;
using EfCorePlus;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CustomFilter.EntityFrameworkCore
{
    public class MyDbContext : DbContext, IEfCorePlusDbContext
    {
        public string? CurrentLanguage => CultureInfo.CurrentCulture.Name;

        public DbSet<TestData> TestDatas { get; set; }

        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ConfigureGlobalFilters(this);
        }

        public string GetCompiledQueryCacheKey()
        {
            return CurrentLanguage ?? "Null";
        }
    }
}
