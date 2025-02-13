using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq.Expressions;

namespace EfCorePlus.Filters
{
    /// <summary>
    /// Interface for defining a filter, filter based on EfCore DbFunction, see https://learn.microsoft.com/en-us/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Should filter the entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entityType"></param>
        /// <returns></returns>
        bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class;

        /// <summary>
        /// build filter DbFunction argument expression, it will be used to <see cref="BuildDbFunctionTranslation"></see> method
        /// Note: the DbFunction first argument is fixed to be the entity type fullname,not need to add it
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="dbContext">You can construct DbFunction arguments using some of the properties in the dbContext instance, such as the current tenant Id etc.</param>
        /// <param name="e">e is an entity parameter to a lambda expression, like: (Entity)e=>xxxx</param>
        /// <returns></returns>
        IEnumerable<Expression> BuildDbFunctionArgs<TDbContext>(TDbContext dbContext, Expression e) where TDbContext : DbContext, IEfCorePlusDbContext;

        /// <summary>
        /// build filter DbFunction translation
        /// Note: the args first element is fixed to be the entity type fullname, next elements are from <see cref="BuildDbFunctionArgs"></see> method
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        SqlExpression BuildDbFunctionTranslation(IReadOnlyList<SqlExpression> args);
    }
}
