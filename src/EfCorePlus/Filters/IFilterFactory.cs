namespace EfCorePlus.Filters
{
    public interface IFilterFactory
    {
        IEnumerable<IFilter> GetAllFilters();
    }
}
