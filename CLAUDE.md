# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NuGet library (`Benday.EfCore.SqlServer`) providing base classes for the repository pattern,
adapter pattern, and service layer pattern with EF Core and SQL Server. **Targets .NET 10 / EF Core
10.** Depends on `Benday.Common.Interfaces` for the shared contracts (`IEntityIdentity<int>`,
`IDeleteable`, `IAsyncReadableRepository<T,int>`, `IAsyncService<T,int>`) — the same interface layer
used by `Benday.CosmosDb`, so storage backends are interchangeable behind the contract.

A companion package `Benday.EfCore.SqlServer.Testing` ships test doubles (`InMemoryRepository<T>`,
`FakeValidatorStrategy<T>`, `FakeUsernameProvider`).

## Build and Test Commands

```bash
# Build the whole solution
dotnet build Benday.EfCore.SqlServer.slnx

# Run unit tests (in-memory, no external dependencies)
dotnet test Benday.EfCore.SqlServer.UnitTests

# Run integration tests (requires SQL Server at localhost with sa/Pa$$word)
dotnet test Benday.EfCore.SqlServer.IntegrationTests

# Run a single test
dotnet test Benday.EfCore.SqlServer.IntegrationTests --filter "FullyQualifiedName~MethodName"

# Generate NuGet packages (auto-generates on build via GeneratePackageOnBuild)
dotnet pack Benday.EfCore.SqlServer
```

### Local SQL Server (for integration tests)

```powershell
./start-local-dev.ps1            # start SQL Server in Docker (sql_server container)
./start-local-dev.ps1 -Pull      # also pull the latest image
./start-local-dev.ps1 -Remove    # recreate the container
```

The `benday-efcore-sqlserver` database + schema are created automatically by EF Core migrations on
first test run. When SQL Server is unreachable the integration tests **skip** (not fail), and the
project sets `--ignore-exit-code 8` so an all-skipped run still exits 0.

## Architecture

Types live in feature namespaces under `Benday.EfCore.SqlServer.*`:

- **Entities/** — `EntityBase` (Id + IsMarkedForDelete + `GetDependentEntities()`, implements
  `IEntityIdentity<int>`, `IDeleteable`, `IEntityWithDependents`); `CoreFieldsEntityBase` (adds audit
  fields + `[Timestamp]` concurrency); `IDependentEntityCollection` + `DependentEntityCollection<T>`
  (child save/delete lifecycle: removes `IsMarkedForDelete` items, prunes after save, exposes
  `GetItems()`).
- **DomainModels/** — `DomainModelBase` (Id only); `CoreFieldsDomainModelBase` (audit fields +
  `Timestamp` that cross the adapter boundary).
- **Adapters/** — `AdapterBase<TModel, TEntity>`: single-item adapt both directions, collection
  merge (match by Id, add new, mark missing for delete), `BeforeAdapt`/`AfterAdapt` hooks, and an
  `AdapterAction` enum (Adapt/Skip/Delete). Subclasses implement `PerformAdapt`.
- **Repositories/** — `SqlEntityFrameworkRepositoryBase<TEntity, TDbContext>` (owns the DbContext,
  `IDisposable`, `VerifyItemIsAddedOrAttached` for add-vs-attach by `Id == 0`); and
  `SqlEntityFrameworkCrudRepositoryBase<TEntity, TDbContext>` (async `GetAllAsync`/`GetByIdAsync`/
  `SaveAsync`/`DeleteAsync`, implements `IAsyncReadableRepository<TEntity,int>`). The save lifecycle
  runs `BeforeSave` → dependent-collection `BeforeSave` → `SaveChangesAsync` → dependent-collection
  `AfterSave` → `AfterSave`. Override `AddIncludes`/`AddDefaultSort` for eager-loading and sorting.
- **ServiceLayers/** — `IValidatorStrategy<T>`, `DefaultValidatorStrategy<T>` (DataAnnotations),
  `IUsernameProvider`; `ServiceLayerBase<TModel, TEntity>` (validate → get/create → adapt → save →
  copy back Id, implements `IAsyncService<TModel,int>`); `CoreFieldsServiceLayerBase<TModel, TEntity>`
  (populates CreatedBy/Date on insert, LastModifiedBy/Date on every save, copies audit fields +
  Timestamp back after save); `InvalidObjectException` / `UnknownObjectException`.
- **Registration/** — `EfCoreRegistrationHelper<TDbContext>` + `services.AddBendayEfCore<TDbContext>(...)`.
  Methods: `UseConnectionString`, `ConfigureDbContext`, `RegisterDbContext`, `RegisterRepository`,
  `RegisterAdapter`, `RegisterService`, `RegisterValidator`, `RegisterDefaultValidator`,
  `RegisterUsernameProvider`, and `RegisterAggregate` (repo + adapter + default validator + service in
  one call — note it does NOT register an `IUsernameProvider`, so add `RegisterUsernameProvider` for
  `CoreFields` services).

### The aggregate / dependent-entity lifecycle (load-bearing)

An aggregate root overrides `GetDependentEntities()` to wrap each child collection in a
`DependentEntityCollection<T>`. The repository's `SaveAsync` calls `BeforeSave`/`AfterSave` on each
collection around `SaveChangesAsync` so children marked `IsMarkedForDelete` are deleted from the DB
and pruned from memory. Don't break this ordering.

### Optimistic concurrency gotcha

`CoreFieldsEntityBase` has a `[Timestamp]` rowversion. A detached `CoreFields` entity **cannot** be
blind-attached-and-updated (EF has no original token → 0 rows affected → `DbUpdateConcurrencyException`).
Load the entity first (so it carries its token), then modify and save — which is what the service
layer does. Plain `EntityBase` entities (no token) can be attach-updated while detached.

## Solution Structure

- `Benday.EfCore.SqlServer/` — the library (NuGet package source)
- `Benday.EfCore.SqlServer.Testing/` — test doubles package (`InMemoryRepository<T>`, fakes)
- `Benday.EfCore.SqlServer.TestApi/` — worked example (Person aggregate: entity, domain model,
  adapter, repository, service, DI wiring) used by the integration tests
- `Benday.EfCore.SqlServer.UnitTests/` — xUnit unit tests (in-memory, no DB)
- `Benday.EfCore.SqlServer.IntegrationTests/` — xUnit integration tests (require SQL Server)

## Testing Notes

- Test framework is **xUnit (xunit.v3)**, aligned with `Benday.Common.Testing` (`TestClassBase`,
  `AssertThat`, `.ShouldEqual`). Test projects are `OutputType=Exe` with
  `TestingPlatformDotnetTestSupport=true` so `dotnet test` works.
- Integration tests expect SQL Server at `localhost`, database `benday-efcore-sqlserver`, user `sa`,
  password `Pa$$word`; they self-skip when it's unavailable.
- `TestApi` is the reference implementation showing how to build a complete aggregate (repository +
  adapter + `CoreFieldsServiceLayerBase` service) on top of the library.
- The library has XML doc comments on all public types/members (`GenerateDocumentationFile`).
