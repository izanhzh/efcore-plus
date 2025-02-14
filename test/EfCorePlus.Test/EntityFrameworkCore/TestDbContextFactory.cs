using EfCorePlus.Filters.Standard;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace EfCorePlus.Test.EntityFrameworkCore
{
    public class TestDbContextFactory : IDisposable
    {
        private readonly SqliteConnection connection;

        public TestDbContextFactory()
        {
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
        }

        public TestDbContext CreateContext()
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .AddEfCorePlus(options =>
                {
                    options.RegisterFilter<SoftDeleteFilter>();
                    options.RegisterFilter<TenantFilter>();
                    options.RegisterFilter<IsActiveFilter>();
                })
                .UseLazyLoadingProxies();
            dbContextOptionsBuilder.AddInterceptors(new SqliteCommandInterceptor());
            var options = dbContextOptionsBuilder.Options;
            var context = new TestDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
