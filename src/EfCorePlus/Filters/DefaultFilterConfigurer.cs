using EfCorePlus.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace EfCorePlus.Filters
{
    public class DefaultFilterConfigurer : IFilterConfigurer
    {
        private static readonly MethodInfo ConfigureEntityGlobalFiltersMethodInfo = typeof(DefaultFilterConfigurer).GetMethod(nameof(ConfigureEntityGlobalFilters), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private readonly IFilterFactory _filterFactory;

        public DefaultFilterConfigurer(IFilterFactory filterFactory)
        {
            _filterFactory = filterFactory;
        }

        public virtual void ConfigureGlobalFilters<TDbContext>(ModelBuilder modelBuilder, TDbContext dbContext)
            where TDbContext : DbContext, IEfCorePlusDbContext
        {
            var filters = _filterFactory.GetAllFilters().ToArray();
            if (filters.Length == 0)
            {
                return;
            }

            // 遍历所有的实体类型, 为每个实体类型配置全局过滤器
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                ConfigureEntityGlobalFiltersMethodInfo.MakeGenericMethod(typeof(TDbContext), entityType.ClrType).Invoke(this, [dbContext, modelBuilder, entityType, filters]);
            }

            // 注册过滤器的DbFunction
            RegisterFilterDbFunction(modelBuilder, filters, dbContext.GetService<EfCorePlusCurrentDbContextProvider>());
        }

        protected virtual void RegisterFilterDbFunction(ModelBuilder modelBuilder, IEnumerable<IFilter> filters, EfCorePlusCurrentDbContextProvider currentDbContextProvider)
        {
            foreach (var filter in filters)
            {
                var dbFunctionMethod = filter.GetType().GetMethod("DbFunction", BindingFlags.Static | BindingFlags.Public)!;
                modelBuilder.HasDbFunction(dbFunctionMethod)
                    .HasTranslation(args =>
                    {
                        /*
                         * 注意：
                         * 不能通过RegisterFilterDbFunction参数传入DbContext给HasTranslation使用
                         * 因为HasTranslation的执行时机和RegisterFilterDbFunction方法的调用时机不是同步的
                         * HasTranslation是在表达式翻译时调用的，所以需要通过EfCorePlusCurrentDbContextProvider，从表达式翻译的过程中获取当前的DbContext
                         */

                        // 第一个参数固定为Entity Type的全名，是一个字符串常量
                        var entityType = (string)((SqlConstantExpression)args[0]).Value!;

                        var isFilterDisabled = currentDbContextProvider.Current?.GetService<EfCorePlusCurrentFilterManagerProvider>().Current?.IsFilterDisabled(filter.GetType(), entityType);
                        if (isFilterDisabled == true)
                        {
                            // empty where sql
                            return new SqlConstantExpression(true, new BoolTypeMapping("bool", DbType.Boolean));
                        }
                        else
                        {
                            return filter.BuildDbFunctionTranslation(args);
                        }
                    });
            }
        }

        protected virtual void ConfigureEntityGlobalFilters<TDbContext, TEntity>(TDbContext dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType, IEnumerable<IFilter> filters)
            where TDbContext : DbContext, IEfCorePlusDbContext
            where TEntity : class
        {
            Expression<Func<TEntity, bool>>? filterExpression = null;
            foreach (var filter in filters)
            {
                if (filter.ShouldFilterEntity<TEntity>(entityType))
                {
                    var dbFunctionMethod = filter.GetType().GetMethod("DbFunction", BindingFlags.Static | BindingFlags.Public)!;
                    var dbFunctionParameters = dbFunctionMethod.GetParameters()!;

                    // 创建参数表达式
                    var e = Expression.Parameter(typeof(TEntity), "e");

                    // 创建第一个固定的常量entityType的表达式
                    var arg0 = Expression.Constant(entityType.ClrType.FullName, typeof(string));

                    // 创建DbFunction方法的其他参数表达式
                    var arg1_n = filter.BuildDbFunctionArgs(dbContext, e);

                    // 合并参数表达式
                    var args = new List<Expression> { arg0 };
                    args.AddRange(arg1_n);

                    //校验参数数量是否正确
                    if (args.Count != dbFunctionParameters.Length)
                    {
                        throw new Exception($"Filter `{filter.GetType().Name}` method 'DbFunction' parameter count is not match, expect {dbFunctionParameters.Length}, actual {args.Count}");
                    }

                    // 创建调用DbFunction方法的表达式
                    var dbFunctionCall = Expression.Call(dbFunctionMethod, args);

                    // 创建 lambda 表达式: e => DbFunction(entityType) 或 e => DbFunction(entityType, arg1, arg2, ...)
                    var lambda = Expression.Lambda<Func<TEntity, bool>>(dbFunctionCall, e);

                    filterExpression = filterExpression == null ? lambda : CombineExpressions(filterExpression, lambda);
                }
            }
            if (filterExpression != null)
            {
                modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
            }
        }

        protected virtual Expression<Func<T, bool>>? CombineExpressions<T>(Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2)
        {
            return ExpressionCombiner.Combine(expression1, expression2);
        }
    }
}
