using EfCorePlus.Filters;
using EfCorePlus.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EfCorePlus
{
    public static class EfCorePlusExtensions
    {
        public static ObjectDisposeAcitonWrapper<FilterManager> CreateFilterManager<TDbContext>(this TDbContext dbContext) where TDbContext : DbContext, IEfCorePlusDbContext
        {
            return dbContext.GetService<EfCorePlusCurrentFilterManagerProvider>().Create();
        }

        public static void DisableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper, Type filterType)
        {
            filterDisablerWrapper.Object.DisableFilter(filterType);
        }

        public static void DisableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper)
            where TFilter : IFilter
        {
            filterDisablerWrapper.Object.DisableFilter<TFilter>();
        }

        public static void EnableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper, Type filterType)
        {
            filterDisablerWrapper.Object.EnableFilter(filterType);
        }

        public static void EnableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper)
            where TFilter : IFilter
        {
            filterDisablerWrapper.Object.EnableFilter<TFilter>();
        }

        public static void DisableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper, Type filterType, params string[] entityType)
        {
            filterDisablerWrapper.Object.DisableFilter(filterType, entityType);
        }

        public static void DisableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper, params Type[] entityType)
            where TFilter : IFilter
        {
            filterDisablerWrapper.Object.DisableFilter<TFilter>(entityType);
        }

        public static void EnableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper, Type filterType, params string[] entityType)
        {
            filterDisablerWrapper.Object.EnableFilter(filterType, entityType);
        }

        public static void EnableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterDisablerWrapper, params Type[] entityType)
            where TFilter : IFilter
        {
            filterDisablerWrapper.Object.EnableFilter<TFilter>(entityType);
        }
    }
}
