using EfCorePlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq.Expressions;

namespace EfCorePlus.Filters.Standard
{
    public class TenantFilter : IFilter
    {
        public static bool DbFunction(string entityType, int tenantId, int? currentTenantId)
        {
            throw new NotSupportedException();
        }

        public virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
        {
            return typeof(ITenant).IsAssignableFrom(typeof(TEntity));
        }

        public virtual IEnumerable<Expression> BuildDbFunctionArgs<TDbContext>(TDbContext dbContext, Expression e) where TDbContext : DbContext, IEfCorePlusDbContext
        {
            yield return Expression.Property(Expression.Convert(e, typeof(ITenant)), "TenantId");// ((ITenant)e).TenantId

            //校验是否有CurrentTenantId属性
            var currentTenantIdProperty = typeof(TDbContext).GetProperty("CurrentTenantId");
            if (currentTenantIdProperty == null)
            {
                throw new ArgumentException("The DbContext must provide a property named 'CurrentTenantId'");
            }
            //校验必须是int?类型
            if (currentTenantIdProperty.PropertyType != typeof(int?))
            {
                throw new ArgumentException("The DbContext.CurrentTenantId must be int? type");
            }

            yield return Expression.Property(Expression.Constant(dbContext), "CurrentTenantId");// dbContext.CurrentTenantId
        }

        public virtual SqlExpression BuildDbFunctionTranslation(IReadOnlyList<SqlExpression> args)
        {
            //(string entityType, int tenantId, int currentTenantId)
            var tenantId = args[1];
            var currentTenantId = args[2];

            //TenantId == CurrentTenantId
            return new SqlBinaryExpression(
                ExpressionType.Equal,
                tenantId,
                currentTenantId,
                tenantId.Type,
                tenantId.TypeMapping);
        }
    }
}
