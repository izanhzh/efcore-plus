using EfCorePlus.Utils;

namespace EfCorePlus
{
    public class EfCorePlusCurrentDbContextProvider
    {
        private readonly AsyncLocal<IEfCorePlusDbContext?> _current = new AsyncLocal<IEfCorePlusDbContext?>();

        public IEfCorePlusDbContext? CurrentDbContext => _current.Value;

        public IDisposable Use(IEfCorePlusDbContext? context)
        {
            var previousValue = CurrentDbContext;
            _current.Value = context;
            return new DisposeAction(() =>
            {
                _current.Value = previousValue;
            });
        }
    }
}
