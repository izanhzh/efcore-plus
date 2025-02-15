# IFilter详解

接口实现实例：
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
接口实现，稍微有一点复杂，让我们逐步探究理解。

EfCore默认是这样注册全局过滤器的：
```csharp
modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);
```

EfCore是这样关闭全局过滤器的：
```csharp
db.Set<MyEntity>().IgnoreQueryFilters();
```

`IgnoreQueryFilters` 一旦使用，就会关闭所有的全局过滤器，无法单独关闭某一个过滤器。

一些聪明的程序员们想到了一种办法，在 `e => e.Language == CurrentLanguage` 这里增加一些判断，比如在DbContext增加一个 `IsLanguageFilterEnabled` 属性，然后将表达式改为 `e => IsLanguageFilterEnabled || e.Language == CurrentLanguage`，这样就可以通过设置 `IsLanguageFilterEnabled` 来单独控制是否启用多语言过滤器。

但是这种方案有一个弊端，那就是生成的SQL语句会多出一个判断条件，大概长这样：`(@__ef_filter__p_0 = CAST(1 AS bit) OR [a].[Language] = @__ef_filter__CurrentLanguage_0)`，这样会严重影响SQL的执行效率。

因此，我们需要一种能够运行时改变生成SQL语句的办法，让其不要参数多余的OR判断条件，遗憾的是，EfCore并没有提供这样的功能，表达式一旦传入给 `HasQueryFilter` 方法，就无法再动态改变了。幸运的是，EfCore提供了一个方法，可以将一个C#方法映射成SQL语句，这就是 `DbFunction`，这就让运行时改变生成SQL语句成为了可能。

> 请先学习了解[将方法映射到自定义 SQL](https://learn.microsoft.com/zh-cn/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql)，这里不再赘述。

最终，我们的目标是要完成这样的绑定：
```csharp
	modelBuilder.Entity<MyEntity>().HasQueryFilter(e => LanguageFilter.DbFunction("MyEntity", e.Language, CurrentLanguage));
```

然后这个 `LanguageFilter.DbFunction` 将会在运行时，根据是否开启关闭过滤器，来生成不同的SQL语句。
- 如果开启，我们要按类似 `modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);` 的配置生成一样的SQL
- 如果关闭，则生成空SQL语句。

现在回到我们的 `LanguageFilter` 类中：

- `DbFunction` 方法是一个必须要提供静态方法，EfCore Plus会将其配置翻译成全局过滤的Where条件SQL语句	
	- 返回值必须是bool
	- 第一个参数固定是 `string entityType`, 这个参数是EfCore Plus内部用来标记当前操作的实体类型
	- 从第二个参数开始，需要你结合你过滤器逻辑进行定义，本例中，第二个参数是 `string language`，表示实体中的语言属性，第三个参数是 `string? currentLanguage`，表示当前的语言参数。

- `ShouldFilterEntity` 方法是用于判断当前实体是否需要应用这个过滤器，EfCore Plus会在扫描所有实体时，调用这个方法，进行过滤器的应用。

- `BuildDbFunctionArgs` 方法是用于构建 `DbFunction` 方法的参数，通过表达式树的方式进行构建
	
	- `TDbContext dbContext` 是当前的DbContext，用于获取 `CurrentLanguage` 属性
	
	- `Expression e` 是当前的实体表达式，用于获取实体的 `Language` 属性`
	
	> 不需要你提供DbFunction方法的第一个参数 `string entityType`，EfCore Plus会自动传入

- `BuildDbFunctionTranslation` 方法用于定义 `DbFunction` 方法如何翻译成SQL语。`IReadOnlyList<SqlExpression> args` 是 `DbFunction` 方法的参数列表，你需要根据这个参数列表，构建一个 `SqlExpression` 对象，用于生成SQL语句