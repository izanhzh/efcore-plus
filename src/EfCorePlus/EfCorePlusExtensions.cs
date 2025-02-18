using EfCorePlus.Filters;
using EfCorePlus.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace EfCorePlus
{
    public static class EfCorePlusExtensions
    {
        public static ObjectDisposeAcitonWrapper<FilterManager> CreateFilterManager<TDbContext>(this TDbContext dbContext) where TDbContext : DbContext, IEfCorePlusDbContext
        {
            var filterFactory = dbContext.GetService<IFilterFactory>();
            var currentFilterManagerProvider = dbContext.GetService<EfCorePlusCurrentFilterManagerProvider>();
            return currentFilterManagerProvider.Create(filterFactory);
        }

        public static void DisableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper, Type filterType)
        {
            filterManagerWrapper.Object.DisableFilter(filterType);
        }

        public static void DisableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper)
            where TFilter : IFilter
        {
            filterManagerWrapper.Object.DisableFilter<TFilter>();
        }

        public static void EnableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper, Type filterType)
        {
            filterManagerWrapper.Object.EnableFilter(filterType);
        }

        public static void EnableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper)
            where TFilter : IFilter
        {
            filterManagerWrapper.Object.EnableFilter<TFilter>();
        }

        public static void DisableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper, Type filterType, params Type[] entityType)
        {
            filterManagerWrapper.Object.DisableFilter(filterType, entityType);
        }

        public static void DisableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper, params Type[] entityType)
            where TFilter : IFilter
        {
            filterManagerWrapper.Object.DisableFilter<TFilter>(entityType);
        }

        public static void EnableFilter(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper, Type filterType, params Type[] entityType)
        {
            filterManagerWrapper.Object.EnableFilter(filterType, entityType);
        }

        public static void EnableFilter<TFilter>(this ObjectDisposeAcitonWrapper<FilterManager> filterManagerWrapper, params Type[] entityType)
            where TFilter : IFilter
        {
            filterManagerWrapper.Object.EnableFilter<TFilter>(entityType);
        }
    }
}
