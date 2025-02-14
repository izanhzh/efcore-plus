using System;
using System.Collections.Generic;

namespace EfCorePlus.Filters
{
    public interface IFilterFactory
    {
        IEnumerable<IFilter> GetAllFilters();

        bool IsRegistered(Type filterType);
    }
}
