using Microsoft.EntityFrameworkCore;

namespace EfCorePlus.Filters
{
    public interface IFilterConfigurer
    {
        void ConfigureGlobalFilters<TDbContext>(ModelBuilder modelBuilder, TDbContext dbContext) where TDbContext : DbContext, IEfCorePlusDbContext;
    }
}
