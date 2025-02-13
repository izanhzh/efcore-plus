using EfCorePlus.Filters.Standard;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .AddEfCorePlus(options =>
                {
                    options.AddFilter<SoftDeleteFilter>();
                    options.AddFilter<TenantFilter>();
                })
            .Options;
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
