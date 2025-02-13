using EfCorePlus.Utils;
using Microsoft.EntityFrameworkCore;

namespace EfCorePlus
{
    public class EfCorePlusCurrentDbContextProvider
    {
        private readonly AsyncLocal<DbContext?> _current = new AsyncLocal<DbContext?>();

        public DbContext? Current => _current.Value;

        public IDisposable Use(DbContext? context)
        {
            var previousValue = Current;
            _current.Value = context;
            return new DisposeAction(() =>
            {
                _current.Value = previousValue;
            });
        }
    }
}
