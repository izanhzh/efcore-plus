using EfCorePlus.Filters;
using EfCorePlus.Utils;

namespace EfCorePlus
{
    internal class EfCorePlusCurrentFilterManagerProvider
    {
        private static readonly AsyncLocal<FilterManager?> _current = new AsyncLocal<FilterManager?>();

        public FilterManager? Current => _current.Value;

        public ObjectDisposeAcitonWrapper<FilterManager> Create()
        {
            if (_current.Value != null)
            {
                throw new InvalidOperationException("A filter manager is already active.");
            }

            _current.Value = new FilterManager();
            return new ObjectDisposeAcitonWrapper<FilterManager>(_current.Value, () => _current.Value = null); ;
        }
    }
}
