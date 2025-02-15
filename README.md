# EfCore Plus

这是一个基于 Entity Framework Core 的扩展库，提供了一些自定义的功能，以便更好地使用 Entity Framework Core。

## 全局过滤增强

Entity Framework Core 默认的全局过滤功能存在一些局限性，使用起来不够方便。

- 配置较为繁琐：如果许多实体具有相同的过滤条件（例如：软删除），需要逐个配置，无法自动扫描所有实体进行自动配置。
- 无法单独关闭某一个过滤条件：如果对一个实体设置了多种过滤条件，只能关闭所有过滤条件。
  > 此问题自 2017 年起就有人在官方反馈中提出，截至目前，Entity Framework Core 仍未提供最终解决方案，详细请参阅 [官方的改进方案讨论](https://github.com/dotnet/efcore/issues/8576)。
- 不能全局关闭所有操作实体的过滤：在一个业务操作工作单元中，必须在每个实体操作中单独手动关闭过滤。

EfCore Plus 提供了更加灵活的全局过滤功能。

[查看文档](docs/GlobalQueryFilters.md)