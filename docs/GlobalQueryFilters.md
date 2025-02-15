# Global Query Filters Plus

This feature implements global filters based on `DbFunction` To use this library effectively, you should first understand how to use `DbFunction` .

Please refer to: [Mapping a method to a custom SQL](https://learn.microsoft.com/en/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql)。

If you are familiar with C# expression trees `Expression`, that's even better. If not, don't worry; powerful AI tools can help you construct the expressions you need.

## Usage

1. Add NuGet Package
```nuget
dotnet add package EfCorePlus
```

2. Inherit your DbContext from `IEfCorePlusDbContext` and implement its methods
```csharp
public class MyDbContext : DbContext, IEfCorePlusDbContext
{
	public int? CurrentTenantId { get; set; }

	public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
	{
	}

	public string GetCompiledQueryCacheKey()
	{
		return CurrentTenantId?.ToString() ?? "Null";
	}
}
```

The `GetCompiledQueryCacheKey` method controls the cache key for compiled SQL statements. For example, if you want to filter data by tenant ID, you can use the current tenant ID as the cache key. When the current tenant ID changes, it will trigger recompilation and generate new filtered SQL statements.

> Note: When dynamic parameters are used as filtering conditions, ensure they are part of the cache key in the `GetCompiledQueryCacheKey` method.

3. Add global filters in the `OnModelCreating` method

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
	base.OnModelCreating(modelBuilder);
	modelBuilder.ConfigureGlobalFilters(this);
}
```

4. Configure EfCorePlus
```csharp
services.AddDbContext<MyDbContext>(options =>
{
	options
		.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
		.AddEfCorePlus(options =>
		{
			options.RegisterFilter<SoftDeleteFilter>();
			options.RegisterFilter<TenantFilter>();
			// Add your custom filters here
		});
});
```

## Standard Filters

### Soft Delete Filter: `SoftDeleteFilter`
To use the soft delete filter, implement the `ISoftDelete` interface in your entities. EF Core will automatically filter out entities where `IsDeleted` is  `true` .
```csharp
public class MyEntity : ISoftDelete
{
	public int Id { get; set; }
	public string Name { get; set; }
	public bool IsDeleted { get; set; }
}
```

### Multi-Tenant Filter: `TenantFilter`
1. Implement the `ITenant` interface in your entities
```csharp
public class MyEntity : ITenant
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int TenantId { get; set; }
}
```

2. Add a  `CurrentTenantId` property in your DbContext and include it in the `GetCompiledQueryCacheKey`
```csharp
public class MyDbContext : DbContext, IEfCorePlusDbContext
{
	public int? CurrentTenantId { get; set; }
	public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
	{
	}
	public string GetCompiledQueryCacheKey()
	{
		return CurrentTenantId?.ToString() ?? "Null";
	}
}
```

3. Set the `CurrentTenantId` property when querying data
```csharp
using (var context = new MyDbContext())
{
	context.CurrentTenantId = 1;
	var data = context.Set<MyEntity>().ToList();
}
```

You don't necessarily need to manually set `CurrentTenantId` every time. You can inject the context storing the current tenant information into the DbContext and bind it to  `CurrentTenantId`.

For example, store the current tenant ID in `HttpContext` and retrieve it in your DbContext.

```csharp
public class MyDbContext : DbContext, IEfCorePlusDbContext
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public MyDbContext(DbContextOptions<MyDbContext> options,IHttpContextAccessor httpContextAccessor)) : base(options)
	{
		_httpContextAccessor = httpContextAccessor;
	}	

	public int? CurrentTenantId => _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;

	public string GetCompiledQueryCacheKey()
	{
		return CurrentTenantId?.ToString() ?? "Null";
	}
}
```

## Custom Filters
Let's create a multi-language filter as an example.

1. Create an interface to mark entities that require language filtering
```csharp
public interface ILanguage
{
	string Language { get; set; }
}
```

2. Create a multi-language filter
```csharp
public class LanguageFilter : IFilter
{
	public static bool DbFunction(string entityType, string language, string? currentLanguage)
	{
		throw new NotSupportedException();
	}

	public virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
	{
		return typeof(ILanguage).IsAssignableFrom(typeof(TEntity));
	}

	public virtual IEnumerable<Expression> BuildDbFunctionArgs<TDbContext>(TDbContext dbContext, Expression e) where TDbContext : DbContext, IEfCorePlusDbContext
	{
		yield return Expression.Property(Expression.Convert(e, typeof(ILanguage)), "Language");// ((ILanguage)e).Language

		var currentLanguageProperty = typeof(TDbContext).GetProperty("CurrentLanguage");
		if (currentLanguageProperty == null)
		{
			throw new ArgumentException("The DbContext must provide a property named 'CurrentLanguage'");
		}
		if (currentLanguageProperty.PropertyType != typeof(string))
		{
			throw new ArgumentException("The DbContext.CurrentLanguage must be string? type");
		}

		yield return Expression.Property(Expression.Constant(dbContext), "CurrentLanguage");// dbContext.CurrentLanguage
	}

	public virtual SqlExpression BuildDbFunctionTranslation(IReadOnlyList<SqlExpression> args)
	{
		// (string entityType, string language, string? currentLanguage)
		var language = args[1];
		var currentLanguage = args[2];

		// ((ILanguage)e).Language == dbContext.CurrentLanguage
		return new SqlBinaryExpression(
			ExpressionType.Equal,
			language,
			currentLanguage,
			language.Type,
			language.TypeMapping);
	}
}
```
[IFilter Details](IFilter.md)

3. Add a `CurrentLanguage` property in your DbContext and include it in the `GetCompiledQueryCacheKey`
```csharp
public class MyDbContext : DbContext, IEfCorePlusDbContext
{
	public int? CurrentTenantId { get; set; }
	public string? CurrentLanguage { get; set; }

	public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
	{
	}

	public string GetCompiledQueryCacheKey()
	{
		return $"{CurrentTenantId?.ToString() ?? "Null"}{CurrentLanguage ?? "Null"}";
	}
}
```

4. Modify EfCorePlus Configuration
```csharp
services.AddDbContext<MyDbContext>(options =>
{
	options
		.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
		.AddEfCorePlus(options =>
		{
			options.RegisterFilter<LanguageFilter>();
		});
});
```

5. Use the multi-language filter
```csharp
using (var context = new MyDbContext())
{
	context.CurrentLanguage = "zh-CN";

	var data = context.Set<MyEntity>().ToList();
	// SELECT * FROM MyEntity WHERE Language = 'zh-CN'
}
```

## Disabling Filters

Assume the following sample entities:
```csharp
public class Blog : ISoftDelete, ITenant
{
	public int Id { get; set; }
	public string Title { get; set; }
	public bool IsDeleted { get; set; }
	public int TenantId { get; set; }
}

public class User : ISoftDelete, ITenant
{
	public int Id { get; set; }
	public string Name { get; set; }
	public bool IsDeleted { get; set; }
	public int TenantId { get; set; }
}
```

### Disable Filters for All Entities
```csharp
using (var context = new MyDbContext())
{
	context.CurrentTenantId = 1;

	var blogs = context.Blogs.ToList();
	// SELECT * FROM Blogs WHERE IsDeleted = 0 AND TenantId = 1

	var users = context.Users.ToList();
	// SELECT * FROM Users WHERE IsDeleted = 0 AND TenantId = 1

	using (var fm = context.CreateFilterManager())
	{
		fm.DisableFilter<SoftDeleteFilter>();

		blogs = context.Blogs.ToList();
		// SELECT * FROM Blogs WHERE TenantId = 1

		users = context.Users.ToList();
		// SELECT * FROM Users WHERE TenantId = 1

		fm.DisableFilter<TenantFilter>();

		blogs = context.Blogs.ToList();
		// SELECT * FROM Blogs

		users = context.Users.ToList();
		// SELECT * FROM Users

		fm.EnableFilter<SoftDeleteFilter>();

		blogs = context.Blogs.ToList();
		// SELECT * FROM Blogs WHERE IsDeleted = 0

		users = context.Users.ToList();
		// SELECT * FROM Users WHERE IsDeleted = 0
	}

	blogs = context.Blogs.ToList();
	// SELECT * FROM Blogs WHERE IsDeleted = 0 AND TenantId = 1

	users = context.Users.ToList();
	// SELECT * FROM Users WHERE IsDeleted = 0 AND TenantId = 1
}
```

### Disable Filters for a Single Entity
```csharp
using (var context = new MyDbContext())
{
	context.CurrentTenantId = 1;

	var blogs = context.Blogs.ToList();
	// SELECT * FROM Blogs WHERE IsDeleted = 0 AND TenantId = 1

	var users = context.Users.ToList();
	// SELECT * FROM Users WHERE IsDeleted = 0 AND TenantId = 1

	using (var fm = context.CreateFilterManager())
	{
		fm.DisableFilter<SoftDeleteFilter>(typeof(Blog));

		blogs = context.Blogs.ToList();
		// SELECT * FROM Blogs WHERE TenantId = 1

		users = context.Users.ToList();
		// SELECT * FROM Users WHERE IsDeleted = 0 AND TenantId = 1
	}

	blogs = context.Blogs.ToList();
	// SELECT * FROM Blogs WHERE IsDeleted = 0 AND TenantId = 1

	users = context.Users.ToList();
	// SELECT * FROM Users WHERE IsDeleted = 0 AND TenantId = 1
}
```

### Important Notes
1. Do not call `CreateFilterManager`  multiple times on the same DbContext instance.
2. Use a `using`  statement to ensure filters revert to their initial state after the block ends.
3. If you disable all entity filters, you cannot disable individual entity filters, and vice versa.