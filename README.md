# EfCore Plus

这是一个基于 Entity Framework Core 的扩展库，提供了一些自定义的功能，以便更好地使用 Entity Framework Core。

## 全局过滤增强

EfCore默认的全局过滤的功能还是比较有限，如果我们设置了多种过滤条件，不能单独的关闭某一个过滤条件，只能关闭所有的过滤条件。EfCore Plus提供了更加灵活的全局过滤功能。

此库是基于 `DbFunction` 来实现全局过滤器的，你需要先掌握理解 `DbFunction` 的用法，才能更好的使用此库。

请先参考阅读理解：[将方法映射到自定义 SQL](https://learn.microsoft.com/zh-cn/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql)。

你如果会C#表达式树 `Expression` 那就更好了，如果你不会，也不用担心，借助一些强大的AI工具，你可以很快构造出你想要的表达式。

> 注意：本类库实现的方案可能是一个临时过渡的方案，详细请了解[EfCore官方的改进方案讨论](https://github.com/dotnet/efcore/issues/8576)

### 使用方法

1. 添加Nuget包
```nuget
dotnet add package EfCorePlus
```

2. 将你的DbContext继承自IEfCorePlusDbContext, 实现接口中的方法
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

> 请注意：当你有数据会作为过滤条件的动态参数时，你需要注意将其作为缓存Key的一部分，在`GetCompiledQueryCacheKey` 方法中将其添加上去。

3. 在OnModelCreating方法中添加全局过滤

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

### 使用内置的过滤器

#### 软删除过滤器 `SoftDeleteFilter`
只需要将你的实体，增加`ISoftDelete`接口实现，即可使用软删除过滤器，Ef Core在执行查询的时候会自动过滤掉该实体中 `IsDeleted` 为 `true` 的数据。
```csharp
public class MyEntity : ISoftDelete
{
	public int Id { get; set; }
	public string Name { get; set; }
	public bool IsDeleted { get; set; }
}
```

#### 多租户过滤器 `TenantFilter`
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

例如：将当前租户ID保存到`HttpContext`中，然后在你的DbContext中，通过`HttpContext`获取当前租户ID。

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

### 自定义过滤器
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
		if (currentLanguageProperty.PropertyType != typeof(string?))
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
这里稍微有一点复杂，让我详细为逐步解释一下：

EfCore默认是这样注册全局过滤器的：
```csharp
modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);
```

EfCore是这样关闭全局过滤器的：
```csharp
db.Set<MyEntity>().IgnoreQueryFilters();
```

`IgnoreQueryFilters` 一旦使用，就会关闭所有的全局过滤器，无法单独关闭某一个过滤器。

一些聪明的程序想到了一种办法，在 `e => e.Language == CurrentLanguage` 这里增加一些判断，在DbContext增加一个 `IsLanguageFilterEnabled` 属性，然后将表达式改为 `e => IsLanguageFilterEnabled || e.Language == CurrentLanguage`，这样就可以通过设置 `IsLanguageFilterEnabled` 来单独控制是否启用多语言过滤器。

但是这种方案有一个弊端，那就是生成的SQL语句会多出一个判断条件，大概长这样：`(@__ef_filter__p_0 = CAST(1 AS bit) OR [a].[Language] = @__ef_filter__CurrentLanguage_0)`，这样会严重影响SQL的执行效率。

因此，我们需要一种能够运行时改变生成SQL语句的办法，让其不要参数多余的OR判断条件，遗憾的是，EfCore并没有提供这样的功能，表达式一旦传入给 `HasQueryFilter` 方法，就无法再动态改变了。幸运的是，EfCore提供了一个方法，可以将一个C#方法映射成SQL语句，这就是 `DbFunction`，这就让运行时改变生成SQL语句成为了可能。

> 请先学习了解[将方法映射到自定义 SQL](https://learn.microsoft.com/zh-cn/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql)，这里我不过多描述。

最终，我们的目标是要完成这样的绑定：
```csharp
	modelBuilder.Entity<MyEntity>().HasQueryFilter(e => LanguageFilter.DbFunction("MyEntity", e.Language, CurrentLanguage));
```

然后这个 `LanguageFilter.DbFunction` 将会在运行时，根据是否开启关闭过滤器，来生成不同的SQL语句。
- 如果开启，我们要按类似 `modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);` 的配置生成一样的SQL
- 如果关闭，则生成空sql语句。

现在回到我们的 `LanguageFilter` 类中：

- `DbFunction` 方法是一个必须要提供静态方法，EfCore Plus会将其配置翻译成全局过滤的Where条件SQL语句。	
	- 返回值必须是bool
	- 第一个参数固定是 `string entityType`, 这个参数是EfCore Plus内部用来标记当前操作的实体类型
	- 从第二个参数开始，需要你结合你过滤器逻辑进行定义，本例中，第二个参数是 `string language`，表示实体中的语言属性，第三个参数是 `string? currentLanguage`，表示当前的语言参数。
- 