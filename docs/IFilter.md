# IFilter���

�ӿ�ʵ��ʵ����
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
�ӿ�ʵ�֣���΢��һ�㸴�ӣ���������̽����⡣

EfCoreĬ��������ע��ȫ�ֹ������ģ�
```csharp
modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);
```

EfCore�������ر�ȫ�ֹ������ģ�
```csharp
db.Set<MyEntity>().IgnoreQueryFilters();
```

`IgnoreQueryFilters` һ��ʹ�ã��ͻ�ر����е�ȫ�ֹ��������޷������ر�ĳһ����������

һЩ�����ĳ���Ա���뵽��һ�ְ취���� `e => e.Language == CurrentLanguage` ��������һЩ�жϣ�������DbContext����һ�� `IsLanguageFilterEnabled` ���ԣ�Ȼ�󽫱��ʽ��Ϊ `e => IsLanguageFilterEnabled || e.Language == CurrentLanguage`�������Ϳ���ͨ������ `IsLanguageFilterEnabled` �����������Ƿ����ö����Թ�������

�������ַ�����һ���׶ˣ��Ǿ������ɵ�SQL������һ���ж���������ų�������`(@__ef_filter__p_0 = CAST(1 AS bit) OR [a].[Language] = @__ef_filter__CurrentLanguage_0)`������������Ӱ��SQL��ִ��Ч�ʡ�

��ˣ�������Ҫһ���ܹ�����ʱ�ı�����SQL���İ취�����䲻Ҫ���������OR�ж��������ź����ǣ�EfCore��û���ṩ�����Ĺ��ܣ����ʽһ������� `HasQueryFilter` ���������޷��ٶ�̬�ı��ˡ����˵��ǣ�EfCore�ṩ��һ�����������Խ�һ��C#����ӳ���SQL��䣬����� `DbFunction`�����������ʱ�ı�����SQL����Ϊ�˿��ܡ�

> ����ѧϰ�˽�[������ӳ�䵽�Զ��� SQL](https://learn.microsoft.com/zh-cn/ef/core/querying/user-defined-function-mapping#mapping-a-method-to-a-custom-sql)�����ﲻ��׸����

���գ����ǵ�Ŀ����Ҫ��������İ󶨣�
```csharp
	modelBuilder.Entity<MyEntity>().HasQueryFilter(e => LanguageFilter.DbFunction("MyEntity", e.Language, CurrentLanguage));
```

Ȼ����� `LanguageFilter.DbFunction` ����������ʱ�������Ƿ����رչ������������ɲ�ͬ��SQL��䡣
- �������������Ҫ������ `modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.Language == CurrentLanguage);` ����������һ����SQL
- ����رգ������ɿ�SQL��䡣

���ڻص����ǵ� `LanguageFilter` ���У�

- `DbFunction` ������һ������Ҫ�ṩ��̬������EfCore Plus�Ὣ�����÷����ȫ�ֹ��˵�Where����SQL���	
	- ����ֵ������bool
	- ��һ�������̶��� `string entityType`, ���������EfCore Plus�ڲ�������ǵ�ǰ������ʵ������
	- �ӵڶ���������ʼ����Ҫ������������߼����ж��壬�����У��ڶ��������� `string language`����ʾʵ���е��������ԣ������������� `string? currentLanguage`����ʾ��ǰ�����Բ�����

- `ShouldFilterEntity` �����������жϵ�ǰʵ���Ƿ���ҪӦ�������������EfCore Plus����ɨ������ʵ��ʱ������������������й�������Ӧ�á�

- `BuildDbFunctionArgs` ���������ڹ��� `DbFunction` �����Ĳ�����ͨ�����ʽ���ķ�ʽ���й���
	
	- `TDbContext dbContext` �ǵ�ǰ��DbContext�����ڻ�ȡ `CurrentLanguage` ����
	
	- `Expression e` �ǵ�ǰ��ʵ����ʽ�����ڻ�ȡʵ��� `Language` ����`
	
	> ����Ҫ���ṩDbFunction�����ĵ�һ������ `string entityType`��EfCore Plus���Զ�����

- `BuildDbFunctionTranslation` �������ڶ��� `DbFunction` ������η����SQL�`IReadOnlyList<SqlExpression> args` �� `DbFunction` �����Ĳ����б�����Ҫ������������б�����һ�� `SqlExpression` ������������SQL���