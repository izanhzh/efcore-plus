using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace EfCorePlus.Filters
{
    public class DefaultFilterFactory : IFilterFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<EfCorePlusOptions> _options;

        public DefaultFilterFactory(IServiceProvider serviceProvider, IOptions<EfCorePlusOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options;
        }

        public IEnumerable<IFilter> GetAllFilters()
        {
            foreach (var filterType in _options.Value.FilterTypes)
            {
                yield return (IFilter)_serviceProvider.GetRequiredService(filterType);
            }
        }

        public bool IsRegistered(Type filterType)
        {
            return _options.Value.FilterTypes.Contains(filterType);
        }
    }
}
