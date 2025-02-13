using EfCorePlus.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EfCorePlus
{
    public static class EfCorePlusModelBuilderExtensions
    {
        public static void ConfigureGlobalFilters<TDbContext>(this ModelBuilder modelBuilder, TDbContext dbContext) where TDbContext : DbContext, IEfCorePlusDbContext
        {
            var configurator = dbContext.GetService<IFilterConfigurer>();
            if (configurator == null)
            {
                throw new Exception($"No {nameof(IFilterConfigurer)} service found in the {nameof(DbContext)}'s service provider.");
            }
            configurator.ConfigureGlobalFilters(modelBuilder, dbContext);
        }
    }
}
