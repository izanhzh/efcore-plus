using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace EfCorePlus
{
    public static class EfCorePlusDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder<TContext> AddEfCorePlus<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, Action<EfCorePlusOptions> efCorePlusOptionsAction)
            where TContext : DbContext, IEfCorePlusDbContext
        {
            if (efCorePlusOptionsAction == null)
                throw new ArgumentNullException(nameof(efCorePlusOptionsAction));

            var extension = optionsBuilder.Options.FindExtension<EfCorePlusDbContextOptionsExtension>() ?? new EfCorePlusDbContextOptionsExtension(efCorePlusOptionsAction);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
            return optionsBuilder;
        }
    }
}
