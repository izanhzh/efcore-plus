using EfCorePlus.Filters;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCorePlus
{
    public class EfCorePlusDbContextOptionsExtension : IDbContextOptionsExtension
    {
        private readonly Action<EfCorePlusOptions> _efCorePlusOptionsAction;

        public EfCorePlusDbContextOptionsExtension(Action<EfCorePlusOptions> efCorePlusOptionsAction)
        {
            _efCorePlusOptionsAction = efCorePlusOptionsAction;
        }

        public DbContextOptionsExtensionInfo Info => new EfCorePlusDbContextOptionsExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(ICompiledQueryCacheKeyGenerator));
            if (serviceDescriptor != null && serviceDescriptor.ImplementationType != null)
            {
                services.Remove(serviceDescriptor);
                services.AddScoped(serviceDescriptor.ImplementationType);
                services.Add(ServiceDescriptor.Scoped<ICompiledQueryCacheKeyGenerator>(provider =>
                    ActivatorUtilities.CreateInstance<EfCorePlusCompiledQueryCacheKeyGenerator>(provider, (ICompiledQueryCacheKeyGenerator)provider.GetRequiredService(serviceDescriptor.ImplementationType))));
            }

            services.Configure(_efCorePlusOptionsAction);
            var efCorePlusOptions = new EfCorePlusOptions();
            _efCorePlusOptionsAction.Invoke(efCorePlusOptions);
            foreach (var filterType in efCorePlusOptions.FilterTypes)
            {
                services.AddSingleton(filterType);
            }

            services.AddSingleton<IFilterFactory, DefaultFilterFactory>();
            services.AddSingleton<IFilterConfigurer, DefaultFilterConfigurer>();

            services.Replace(ServiceDescriptor.Scoped<IAsyncQueryProvider, EfCorePlusEntityQueryProvider>());
            services.AddSingleton<EfCorePlusCurrentDbContextProvider>();

            services.AddSingleton<EfCorePlusCurrentFilterManagerProvider>();
        }

        public void Validate(IDbContextOptions options)
        {
        }

        private class EfCorePlusDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
        {
            public EfCorePlusDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            public override bool IsDatabaseProvider => false;

#if NETCOREAPP3_1 
            public override long GetServiceProviderHashCode() => 0;
#else
            public override int GetServiceProviderHashCode() => 0;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is EfCorePlusDbContextOptionsExtensionInfo;
#endif

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }

            public override string LogFragment => "EfCorePlusDbContextOptionsExtension";
        }
    }
}
