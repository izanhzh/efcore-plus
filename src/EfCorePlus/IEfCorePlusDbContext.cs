using EfCorePlus.Filters;

namespace EfCorePlus
{
    public interface IEfCorePlusDbContext
    {
        string GetCompiledQueryCacheKey();
    }
}
