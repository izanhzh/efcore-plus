using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EfCorePlus
{
    public class EfCorePlusCompiledQueryCacheKeyGenerator : ICompiledQueryCacheKeyGenerator
    {
        protected ICompiledQueryCacheKeyGenerator InnerCompiledQueryCacheKeyGenerator { get; }

        protected ICurrentDbContext CurrentContext { get; }

        public EfCorePlusCompiledQueryCacheKeyGenerator(
            ICompiledQueryCacheKeyGenerator innerCompiledQueryCacheKeyGenerator,
            ICurrentDbContext currentContext)
        {
            InnerCompiledQueryCacheKeyGenerator = innerCompiledQueryCacheKeyGenerator;
            CurrentContext = currentContext;
        }

        public virtual object GenerateCacheKey(Expression query, bool async)
        {
            var cacheKey = InnerCompiledQueryCacheKeyGenerator.GenerateCacheKey(query, async);

            if (CurrentContext.Context is IEfCorePlusDbContext efCorePlusDbContext)
            {
                var cacheKey1 = CurrentContext.Context.GetService<EfCorePlusCurrentFilterManagerProvider>().Current?.CompiledQueryCacheKey;// 根据当前禁用的过滤器生成的缓存键
                var cacheKey2 = efCorePlusDbContext.GetCompiledQueryCacheKey(); // 根据一些动态属性生成的缓存键，例如当前租户Id等
                return new EfCorePlusCompiledQueryCacheKey(cacheKey, $"{cacheKey1}||{cacheKey2}");
            }

            return cacheKey;
        }

        private readonly struct EfCorePlusCompiledQueryCacheKey : IEquatable<EfCorePlusCompiledQueryCacheKey>
        {
            private readonly object _compiledQueryCacheKey;
            private readonly string _currentFilterCacheKey;

            public EfCorePlusCompiledQueryCacheKey(object compiledQueryCacheKey, string currentFilterCacheKey)
            {
                _compiledQueryCacheKey = compiledQueryCacheKey;
                _currentFilterCacheKey = currentFilterCacheKey;
            }

            public override bool Equals(object? obj)
            {
                return obj is EfCorePlusCompiledQueryCacheKey key && Equals(key);
            }

            public bool Equals(EfCorePlusCompiledQueryCacheKey other)
            {
                return _compiledQueryCacheKey.Equals(other._compiledQueryCacheKey) &&
                       _currentFilterCacheKey == other._currentFilterCacheKey;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_compiledQueryCacheKey, _currentFilterCacheKey);
            }
        }
    }
}
