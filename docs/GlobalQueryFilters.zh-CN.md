# 全局过滤增强

此功能是基于 `DbFunction` 来实现全局过滤器的，你需要先掌握理解 `DbFunction` 的用法，才能更好的使用此库。

请先参考阅读理解：[将方法映射到自定义 SQL](https://learn.microsoft.com/zh-cn/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql)。

如果你会C#表达式树 `Expression` 那就更好了，如果你不会，也不用担心，借助一些强大的AI工具，你可以很快构造出你想要的表达式。

## 使用方法

1. 添加Nuget包
```nuget
dotnet add package EfCorePlus
```

2. 将你的DbContext继承自 `IEfCorePlusDbContext`, 实现接口中的方法
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

`GetCompiledQueryCacheKey` 方法用来控制编译生成SQL语句的缓存Key。例如我们需要根据租户ID过滤数据，那么我们可以将当前租户ID作为缓存Key，这样当前租户ID发生变化时，会重新触发编译，从而生成新的过滤SQL语句。

> 请注意：当你有数据会作为过滤条件的动态参数时，你需要注意将其作为缓存Key的一部分，在 `GetCompiledQueryCacheKey` 方法中将其添加上去。

3. 在 `OnModelCreating` 方法中添加全局过滤

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
	base.OnModelCreating(modelBuilder);
	modelBuilder.ConfigureGlobalFilters(this);
}
```

4. 配置EfCorePlus
```csharp
services.AddDbContext<MyDbContext>(options =>
{
	options
		.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
		.AddEfCorePlus(options =>
		{
			options.RegisterFilter<SoftDeleteFilter>();
			options.RegisterFilter<TenantFilter>();
			// 添加你自定义的过滤器
		});
});
```

## 使用内置的过滤器

### 软删除过滤器: `SoftDeleteFilter`
只需要将你的实体，增加 `ISoftDelete` 接口实现，即可使用软删除过滤器，Ef Core在执行查询的时候会自动过滤掉该实体中 `IsDeleted` 为 `true` 的数据。
```csharp
public class MyEntity : ISoftDelete
{
	public int Id { get; set; }
	public string Name { get; set; }
	public bool IsDeleted { get; set; }
}
```

### 多租户过滤器: `TenantFilter`
1. 将你的实体，增加`ITenant`接口实现
```csharp
public class MyEntity : ITenant
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int TenantId { get; set; }
}
```

2. 在你的DbContext中增加 `CurrentTenantId` 属性，并将 `CurrentTenantId` 属性，作为编译生成SQL语句缓存Key的一部分
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

3. 查询数据时，设置 `CurrentTenantId` 属性
```csharp
using (var context = new MyDbContext())
{
	context.CurrentTenantId = 1;
	var data = context.Set<MyEntity>().ToList();
}
```

`CurrentTenantId` 属性值，并不一定需要在查询时进行手动设置，如果你不想每次都手动设置当前租户ID，你只需要想办法将你用来存储当前租户信息的上下文注入到DbContext中，绑定给`CurrentTenantId`属性即可。

例如：将当前租户ID保存到 `HttpContext` 中，然后在你的DbContext中，通过 `HttpContext` 获取当前租户ID。

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

## 自定义过滤器
我们以自定义一个多语言过滤器为例，来演示如何自定义一个过滤器。

1. 创建一个接口，用来标记需要多语言过滤的实体
```csharp
public interface ILanguage
{
	string Language { get; set; }
}
```

2. 创建一个多语言过滤器
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
[IFilter详解](IFilter.zh-CN.md)

3. 在你的DbContext中增加 `CurrentLanguage` 属性，并将 `CurrentLanguage` 属性，作为编译生成SQL语句缓存Key的一部分
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

4. 更改EfCorePlus配置
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

5. 使用多语言过滤器
```csharp
using (var context = new MyDbContext())
{
	context.CurrentLanguage = "zh-CN";

	var data = context.Set<MyEntity>().ToList();
	// SELECT * FROM MyEntity WHERE Language = 'zh-CN'
}
```

## 关闭过滤器

假设示例实体：
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

### 对全部实体关闭过滤器
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

### 对单个实体关闭过滤器
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

### 注意事项
1. 对同一个DbContext实例对象，不能多次调用 `CreateFilterManager` 方法
2. 请使用 `using` 语句包裹，`using` 语句结束后，过滤器将恢复到初始状态
3. 如果已经使用了关闭全部实体过滤器，那么无法再关闭单个实体过滤器，反之亦然
