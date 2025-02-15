# IFilter Details

Interface implementation example:
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
Interface implementation can be somewhat complex; let's explore and understand it step by step.

By default, Entity Framework Core provides global query filters using annotations:
```csharp
modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);
```

To ignore global query filters in Entity Framework Core:
```csharp
db.Set<MyEntity>().IgnoreQueryFilters();
```

`IgnoreQueryFilters` ignores all global query filters but does not remove specific conditions from the filter.

Some developers may want to add a condition like `e => e.Language == CurrentLanguage` with additional logic. For example, adding a property `IsLanguageFilterEnabled` to the DbContext and modifying the expression to `e => IsLanguageFilterEnabled || e.Language == CurrentLanguage`, This can enable you to control whether the multilingual filter is enabled solely by setting `IsLanguageFilterEnabled`.

However, this approach has a drawback: it adds an extra condition to the generated SQL statement, which looks something like this: `(@__ef_filter__p_0 = CAST(1 AS bit) OR [a].[Language] = @__ef_filter__CurrentLanguage_0)`. This additional condition can significantly impact the performance of SQL execution.

To optimize this, you might want to minimize the generated SQL by avoiding unnecessary OR conditions. However, Entity Framework Core does not provide a direct way to modify `HasQueryFilter` expressions dynamically. Instead, it translates C# expressions into SQL, including user-defined functions via `DbFunction`.

> For more information, refer to the [Mapping a method to a custom SQL](https://learn.microsoft.com/en/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql).

Ultimately, our goal is to achieve this binding:
```csharp
	modelBuilder.Entity<MyEntity>().HasQueryFilter(e => LanguageFilter.DbFunction("MyEntity", e.Language, CurrentLanguage));
```

This approach ensures that `LanguageFilter.DbFunction` is translated into appropriate SQL, generating different SQL based on whether the filter is enabled or disabled:
- When enabled, it generates SQL similar to `modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);` .
- When disabled, it generates simpler SQL without the filter.

Key Components of `LanguageFilter`:

- The `DbFunction` method must be a static method. EfCore Plus will translate its configuration into a global filter Where condition SQL statement.
	- The return value must be `bool`
	- The first parameter is fixed as `string entityType`, which EfCore Plus uses internally to mark the current entity type.
	- From the second parameter onward, you need to define them based on your filter logic. In this example, the second parameter is `string language`, representing the language property of the entity, and the third parameter is `string? currentLanguage`, representing the current language parameter.

- The `ShouldFilterEntity` method is used to determine whether the current entity should apply this filter. EfCore Plus will call this method when scanning all entities to apply the filter.

- The `BuildDbFunctionArgs` method is used to build the parameters for the `DbFunction` method using an expression tree.
	
	- `TDbContext dbContext`  is the current DbContext, used to obtain the `CurrentLanguage` property.
	
	- `Expression e` is the current entity expression, used to obtain the entity's `Language`  property.
	
	> You do not need to provide the first parameter `string entityType` for the `DbFunction` method, EfCore Plus will automatically pass it in.

- The `BuildDbFunctionTranslation`  method is used to define how the `DbFunction` method is translated into SQL. `IReadOnlyList<SqlExpression> args` is the list of parameters for the `DbFunction` method. You need to construct a `SqlExpression` object based on this parameter list to generate the SQL statement.