using EfCorePlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Linq.Expressions;

namespace EfCorePlus.Filters.Standard
{
    public class SoftDeleteFilter : IFilter
    {
        public static bool DbFunction(string entityType, bool isDeleted)
        {
            throw new NotSupportedException();
        }

        public virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
        {
            return typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity));
        }

        public virtual IEnumerable<Expression> BuildDbFunctionArgs<TDbContext>(TDbContext dbContext, Expression e) where TDbContext : DbContext, IEfCorePlusDbContext
        {
            yield return Expression.Property(Expression.Convert(e, typeof(ISoftDelete)), "IsDeleted");// ((ISoftDelete)e).IsDeleted
        }

        public virtual SqlExpression BuildDbFunctionTranslation(IReadOnlyList<SqlExpression> args)
        {
            //(string entityType, bool isDeleted)
            var isDeleted = args[1];

            //IsDeleted == false
            return new SqlBinaryExpression(
                ExpressionType.Equal,
                isDeleted,
                new SqlConstantExpression(false, new BoolTypeMapping("bool", DbType.Boolean)),
                isDeleted.Type,
                isDeleted.TypeMapping);
        }
    }
}
