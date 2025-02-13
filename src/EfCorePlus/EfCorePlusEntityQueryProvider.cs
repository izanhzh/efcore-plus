using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Linq.Expressions;

namespace EfCorePlus
{
#pragma warning disable EF1001
    public class EfCorePlusEntityQueryProvider : EntityQueryProvider
    {
        protected EfCorePlusCurrentDbContextProvider EfCorePlusCurrentDbContextProvider { get; }
        protected ICurrentDbContext CurrentDbContext { get; }

        public EfCorePlusEntityQueryProvider(
            IQueryCompiler queryCompiler,
            EfCorePlusCurrentDbContextProvider efCorePlusCurrentDbContext,
            ICurrentDbContext currentDbContext) : base(queryCompiler)
        {
            EfCorePlusCurrentDbContextProvider = efCorePlusCurrentDbContext;
            CurrentDbContext = currentDbContext;
        }

        public override object Execute(Expression expression)
        {
            using (EfCorePlusCurrentDbContextProvider.Use(CurrentDbContext.Context))
            {
                return base.Execute(expression);
            }
        }

        public override TResult Execute<TResult>(Expression expression)
        {
            using (EfCorePlusCurrentDbContextProvider.Use(CurrentDbContext.Context))
            {
                return base.Execute<TResult>(expression);
            }
        }

        public override TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = new CancellationToken())
        {
            using (EfCorePlusCurrentDbContextProvider.Use(CurrentDbContext.Context))
            {
                return base.ExecuteAsync<TResult>(expression, cancellationToken);
            }
        }
    }
}
#pragma warning restore EF1001
