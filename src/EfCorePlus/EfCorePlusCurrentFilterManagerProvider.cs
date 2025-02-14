using EfCorePlus.Filters;
using EfCorePlus.Utils;
using System;
using System.Threading;

namespace EfCorePlus
{
    internal class EfCorePlusCurrentFilterManagerProvider
    {
        private static readonly AsyncLocal<FilterManager?> _current = new AsyncLocal<FilterManager?>();

        public FilterManager? Current => _current.Value;

        public ObjectDisposeAcitonWrapper<FilterManager> Create(IFilterFactory filterFactory)
        {
            if (_current.Value != null)
            {
                throw new InvalidOperationException("A filter manager is already active.");
            }

            _current.Value = new FilterManager(filterFactory);
            return new ObjectDisposeAcitonWrapper<FilterManager>(_current.Value, () => _current.Value = null); ;
        }
    }
}
