# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NuGet library (`Benday.EfCore.SqlServer`) providing base classes for the repository pattern and domain model pattern with EF Core and SQL Server. Targets .NET 8.0. Depends on `Benday.Common` for interfaces (`IInt32Identity`, `IDeleteable`) and search types (`Search`, `SearchResult`, `SearchArgument`, `SearchMethod`, `SearchOperator`).

## Build and Test Commands

```bash
# Build
dotnet build Benday.EfCore.SqlServer.sln

# Run unit tests (no external dependencies)
dotnet test Benday.EfCore.SqlServer.UnitTests

# Run integration tests (requires SQL Server at localhost with sa/Pa$$word)
dotnet test Benday.EfCore.SqlServer.IntegrationTests

# Run a single test
dotnet test Benday.EfCore.SqlServer.IntegrationTests --filter "FullyQualifiedName~MethodName"

# Generate NuGet package (auto-generates on build via GeneratePackageOnBuild)
dotnet pack Benday.EfCore.SqlServer
```

## Architecture

The library provides a three-level repository inheritance chain:

1. **`SqlEntityFrameworkRepositoryBase<TEntity, TDbContext>`** - Base layer. Owns the `DbContext`, implements `IDisposable`, and provides `VerifyItemIsAddedOrAttachedToDbSet` which handles add-vs-attach based on `Id == 0`.

2. **`SqlEntityFrameworkCrudRepositoryBase<TEntity, TDbContext>`** - CRUD operations (`GetAll`, `GetById`, `Save`, `Delete`). Subclasses must implement `EntityDbSet`. Uses template methods (`BeforeSave`, `AfterSave`, `BeforeDelete`, `AfterDelete`, `BeforeGetAll`, `AddDefaultSort`) for extensibility. Handles dependent/child entity lifecycle via `IEntityBase.GetDependentEntities()`.

3. **`SqlEntityFrameworkSearchableRepositoryBase<TEntity, TDbContext>`** - Adds `Search()` using `Benday.Common.Search`. Subclasses must implement six abstract predicate methods (Contains, StartsWith, EndsWith, Equals, IsNotEqual, DoesNotContain) and two sort methods (AddSortAscending, AddSortDescending) to map property names to LINQ expressions.

Key supporting types:
- **`IEntityBase`** - extends `IInt32Identity` + `IDeleteable`, requires `GetDependentEntities()`
- **`DependentEntityCollection<T>`** - manages parent-child save/delete lifecycle (removes entities marked `IsMarkedForDelete`)
- **`LinqPredicateExtensions`** / **`ParameterSubstitutionVisitor`** - combine LINQ expression predicates with And/Or for building search where clauses

## Solution Structure

- `Benday.EfCore.SqlServer/` - The library (NuGet package source)
- `Benday.EfCore.SqlServer.TestApi/` - Test domain model (Person, PersonNote, TestDbContext, SqlPersonRepository) used by integration tests
- `Benday.EfCore.SqlServer.IntegrationTests/` - MSTest integration tests requiring SQL Server
- `Benday.EfCore.SqlServer.UnitTests/` - MSTest unit tests

## Testing Notes

- Test framework is **MSTest** (not xUnit/NUnit)
- Integration tests expect a SQL Server instance at `localhost` with database `benday-efcore-sqlserver`, user `sa`, password `Pa$$word`
- `SqlPersonRepository` in TestApi is the reference implementation showing how to extend the searchable repository base class
