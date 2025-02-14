using EfCorePlus.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace EfCorePlus.Test.EntityFrameworkCore
{
    public class IsActiveFilter : IFilter
    {
        public static bool DbFunction(string entityType, bool isActive)
        {
            throw new NotSupportedException();
        }

        public bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
        {
            return typeof(IIsActive).IsAssignableFrom(typeof(TEntity));
        }

        public IEnumerable<Expression> BuildDbFunctionArgs<TDbContext>(TDbContext dbContext, Expression e) where TDbContext : DbContext, IEfCorePlusDbContext
        {
            yield return Expression.Property(Expression.Convert(e, typeof(IIsActive)), "IsActive");// ((IIsActive)e).IsActive
        }

        public SqlExpression BuildDbFunctionTranslation(IReadOnlyList<SqlExpression> args)
        {
            // (string entityType, bool isActive)
            var isActive = args[1];

            // ((IIsActive)e).IsActive == true
            return new SqlBinaryExpression(
                ExpressionType.Equal,
                isActive,
#if NET9_0_OR_GREATER
                new SqlConstantExpression(true, new BoolTypeMapping("bool", DbType.Boolean)),
#else
                new SqlConstantExpression(Expression.Constant(true), new BoolTypeMapping("bool")),
#endif
                isActive.Type,
                isActive.TypeMapping);
        }
    }
}
