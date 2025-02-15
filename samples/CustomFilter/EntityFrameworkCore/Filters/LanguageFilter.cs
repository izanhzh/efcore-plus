using CustomFilter.EntityFrameworkCore.Entities;
using EfCorePlus;
using EfCorePlus.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq.Expressions;

namespace CustomFilter.EntityFrameworkCore.Filters
{
    public class LanguageFilter : IFilter
    {
        public static bool DbFunction(string entityType, string language, string? currentLanguage)
        {
            throw new NotSupportedException();
        }

        public virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
        {
            return typeof(ILanguage).IsAssignableFrom(typeof(TEntity));
        }

        public virtual IEnumerable<Expression> BuildDbFunctionArgs<TDbContext>(TDbContext dbContext, Expression e) where TDbContext : DbContext, IEfCorePlusDbContext
        {
            yield return Expression.Property(Expression.Convert(e, typeof(ILanguage)), "Language");// ((ILanguage)e).Language

            var currentLanguageProperty = typeof(TDbContext).GetProperty("CurrentLanguage");
            if (currentLanguageProperty == null)
            {
                throw new ArgumentException("The DbContext must provide a property named 'CurrentLanguage'");
            }
            if (currentLanguageProperty.PropertyType != typeof(string))
            {
                throw new ArgumentException("The DbContext.CurrentLanguage must be string? type");
            }

            yield return Expression.Property(Expression.Constant(dbContext), "CurrentLanguage");// dbContext.CurrentLanguage
        }

        public virtual SqlExpression BuildDbFunctionTranslation(IReadOnlyList<SqlExpression> args)
        {
            // (string entityType, string language, string? currentLanguage)
            var language = args[1];
            var currentLanguage = args[2];

            // ((ILanguage)e).Language == dbContext.CurrentLanguage
            return new SqlBinaryExpression(
                ExpressionType.Equal,
                language,
                currentLanguage,
                language.Type,
                language.TypeMapping);
        }
    }
}
