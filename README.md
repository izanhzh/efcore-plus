[English](README.md) | [中文](README.zh-CN.md)

> This is a translation of the Chinese README. The translation may not be accurate. If you find any errors, please let me know, Welcome PR.

# EfCore Plus

This is an extension library based on Entity Framework Core that provides some custom features for better use of Entity Framework Core.

## Global Query Filter Plus

The default global query filter feature of Entity Framework Core has some limitations and is not convenient to use.

- Complicated configuration: If many entities have the same filter condition (e.g., soft delete), each entity needs to be configured individually, and it cannot automatically scan all entities for automatic configuration.
- Cannot disable a single filter condition: If multiple filter conditions are set for an entity, all filter conditions can only be disabled together.
- Cannot globally disable all filters for operating entities: In a business operation unit, filters must be manually disabled for each entity operation.

> A dynamic summary of official community discussions on such issues: [Improve usability of global query filters](https://github.com/dotnet/efcore/issues/21459)

EfCore Plus provides more flexible global filter functionality.

[View Documentation](docs/GlobalQueryFilters.md)
