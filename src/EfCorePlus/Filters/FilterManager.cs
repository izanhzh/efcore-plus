using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EfCorePlus.Filters
{
    public class FilterManager
    {
        private static readonly object _lock = new object();
        private readonly HashSet<Type> _allEntityDisabledFilters;//filter disabled for all entities
        private readonly ConcurrentDictionary<Type, HashSet<string>> _entityDisabledFilters;//filter disabled for specific entities
        private readonly IFilterFactory _filterFactory;

        public FilterManager(IFilterFactory filterFactory)
        {
            _allEntityDisabledFilters = new HashSet<Type>();
            _entityDisabledFilters = new ConcurrentDictionary<Type, HashSet<string>>();
            _filterFactory = filterFactory;
        }

        public string? CompiledQueryCacheKey { get; private set; }

        public bool IsFilterDisabled(Type filterType, string entityType)
        {
            lock (_lock)
            {
                if (_allEntityDisabledFilters.Contains(filterType))
                {
                    return true;
                }
                else if (_entityDisabledFilters.TryGetValue(filterType, out var entityTypes))
                {
                    return entityTypes.Contains(entityType);
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsFilterDisabled<TFilter, TEntity>()
            where TFilter : IFilter
            where TEntity : new()
        {
            return IsFilterDisabled(typeof(TFilter), typeof(TEntity).FullName!);
        }

        public void DisableFilter(Type filterType)
        {
            lock (_lock)
            {
                if (!typeof(IFilter).IsAssignableFrom(filterType))
                {
                    throw new InvalidOperationException($"The type `{filterType.FullName}` does not implement the interface `{typeof(IFilter).FullName}`.");
                }
                if (!_filterFactory.IsRegistered(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` is not registered.");
                }
                if (_entityDisabledFilters.ContainsKey(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` has been disabled for specific entities, you can't disable it for all entities.");
                }
                if (!_allEntityDisabledFilters.Contains(filterType))
                {
                    _allEntityDisabledFilters.Add(filterType);
                }
                ReBuildCompiledQueryCacheKey();
            }
        }

        public void DisableFilter<TFilter>()
            where TFilter : IFilter
        {
            DisableFilter(typeof(TFilter));
        }

        public void EnableFilter(Type filterType)
        {
            lock (_lock)
            {
                if (!typeof(IFilter).IsAssignableFrom(filterType))
                {
                    throw new InvalidOperationException($"The type `{filterType.FullName}` does not implement the interface `{typeof(IFilter).FullName}`.");
                }
                if (!_filterFactory.IsRegistered(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` is not registered.");
                }
                if (_entityDisabledFilters.ContainsKey(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` has been disabled for specific entities, you can't enable it for all entities.");
                }
                if (_allEntityDisabledFilters.Contains(filterType))
                {
                    _allEntityDisabledFilters.Remove(filterType);
                }
                ReBuildCompiledQueryCacheKey();
            }
        }

        public void EnableFilter<TFilter>()
            where TFilter : IFilter
        {
            EnableFilter(typeof(TFilter));
        }

        public void DisableFilter(Type filterType, params Type[] entityType)
        {
            lock (_lock)
            {
                if (!typeof(IFilter).IsAssignableFrom(filterType))
                {
                    throw new InvalidOperationException($"The type `{filterType.FullName}` does not implement the interface `{typeof(IFilter).FullName}`.");
                }
                if (!_filterFactory.IsRegistered(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` is not registered.");
                }
                if (_allEntityDisabledFilters.Contains(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` has been disabled for all entities, you can't disable it for specific entities.");
                }
                if (!_entityDisabledFilters.TryGetValue(filterType, out var entityTypes))
                {
                    entityTypes = new HashSet<string>();
                    _entityDisabledFilters.TryAdd(filterType, entityTypes);
                }
                foreach (var type in entityType)
                {
                    entityTypes.Add(type.FullName!);
                }
                ReBuildCompiledQueryCacheKey();
            }
        }

        public void DisableFilter<TFilter>(params Type[] entityType)
            where TFilter : IFilter
        {
            DisableFilter(typeof(TFilter), entityType);
        }

        public void EnableFilter(Type filterType, params Type[] entityType)
        {
            lock (_lock)
            {
                if (!typeof(IFilter).IsAssignableFrom(filterType))
                {
                    throw new InvalidOperationException($"The type `{filterType.FullName}` does not implement the interface `{typeof(IFilter).FullName}`.");
                }
                if (!_filterFactory.IsRegistered(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` is not registered.");
                }
                if (_allEntityDisabledFilters.Contains(filterType))
                {
                    throw new InvalidOperationException($"The filter `{filterType.FullName}` has been disabled for all entities, you can't enable it for specific entities.");
                }
                if (_entityDisabledFilters.TryGetValue(filterType, out var entityTypes))
                {
                    foreach (var type in entityType)
                    {
                        entityTypes.Remove(type.FullName!);
                    }
                    if (entityTypes.Count == 0)
                    {
                        entityTypes = new HashSet<string>();
                        _entityDisabledFilters.TryRemove(filterType, out _);
                    }
                }
                ReBuildCompiledQueryCacheKey();
            }
        }

        public void EnableFilter<TFilter>(params Type[] entityType)
            where TFilter : IFilter
        {
            EnableFilter(typeof(TFilter), entityType.ToArray());
        }

        private void ReBuildCompiledQueryCacheKey()
        {
            var key1 = string.Join("|", _allEntityDisabledFilters.Select(s => s.FullName).OrderBy(o => o));
            var key2 = string.Join("|", _entityDisabledFilters.Select(x => x.Key.FullName + ":" + string.Join(",", x.Value)).OrderBy(o => o));
            CompiledQueryCacheKey = $"{key1}&{key2}";
        }
    }
}