using EfCorePlus.Filters;
using System.Reflection;

namespace EfCorePlus
{
    public class EfCorePlusOptions
    {
        public HashSet<Type> FilterTypes { get; private set; }

        public EfCorePlusOptions()
        {
            FilterTypes = new HashSet<Type>();
        }

        public void AddFilter<TFilter>() where TFilter : IFilter, new()
        {
            var dbFunctionMethod = typeof(TFilter).GetMethod("DbFunction", BindingFlags.Static | BindingFlags.Public);
            if (dbFunctionMethod == null)
            {
                throw new ArgumentException($"The Filter `{typeof(TFilter).FullName}` must provide a static method named 'DbFunction' with first parameter type is string and name is entityType");
            }
            var dbFunctionParameters = dbFunctionMethod.GetParameters();
            if (dbFunctionParameters.Length == 0 || dbFunctionParameters[0].ParameterType != typeof(string) || dbFunctionParameters[0].Name != "entityType")
            {
                throw new ArgumentException($"The Filter `{typeof(TFilter).FullName}` must provide a static method named 'DbFunction' with first parameter type is string and name is entityType");
            }
            FilterTypes.Add(typeof(TFilter));
        }
    }
}
