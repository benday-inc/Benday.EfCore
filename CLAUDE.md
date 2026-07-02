# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Two NuGet libraries providing base classes for the repository pattern, adapter pattern, and service
layer pattern with EF Core. **Target .NET 10 / EF Core 10.**

- **`Benday.EfCore`** — the provider-agnostic core: entities, domain models, adapters, the repository
  and service-layer base classes, DI registration, and a design-time DbContext factory base. Works
  against any EF Core provider. References `Benday.Common` (for `SafeToString` and the shared
  contracts `IEntityIdentity<int>`, `IDeleteable`, `IAsyncReadableRepository<T,int>`,
  `IAsyncService<T,int>` — the same interface layer used by `Benday.CosmosDb`, so storage backends
  are interchangeable behind the contract).
- **`Benday.EfCore.SqlServer`** — a thin SQL Server wiring layer on top of `Benday.EfCore`:
  `UseConnectionString`, the `ApplyBendaySqlServerConcurrency` rowversion convention, and
  `SqlServerDesignTimeDbContextFactory`. Keeps only `Microsoft.EntityFrameworkCore.SqlServer`.

A companion package `Benday.EfCore.Testing` ships test doubles (`InMemoryRepository<T>`,
`FakeValidatorStrategy<T>`, `FakeUsernameProvider`) and references `Benday.EfCore`.

## Build and Test Commands

```bash
# Build the whole solution
dotnet build Benday.EfCore.slnx

# Run unit tests (in-memory, no external dependencies)
dotnet test test/Benday.EfCore.SqlServer.UnitTests

# Run integration tests (requires SQL Server at localhost with sa/Pa$$word)
dotnet test test/Benday.EfCore.SqlServer.IntegrationTests

# Run a single test
dotnet test test/Benday.EfCore.SqlServer.IntegrationTests --filter "FullyQualifiedName~MethodName"

# Generate NuGet packages (auto-generates on build via GeneratePackageOnBuild)
dotnet pack src/Benday.EfCore
dotnet pack src/Benday.EfCore.SqlServer
dotnet pack src/Benday.EfCore.Testing
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

The provider-agnostic types live in feature namespaces under `Benday.EfCore.*` (in the `Benday.EfCore`
project). The SQL Server-specific pieces live in `Benday.EfCore.SqlServer` (see the next section).

- **Entities/** — `EntityBase` (Id + IsMarkedForDelete + `GetDependentEntities()`, implements
  `IEntityIdentity<int>`, `IDeleteable`, `IEntityWithDependents`); `CoreFieldsEntityBase` (adds audit
  fields + a `Timestamp` concurrency token — mapped per provider, NOT via `[Timestamp]`; see the
  concurrency section); `IDependentEntityCollection` + `DependentEntityCollection<T>` (child
  save/delete lifecycle: removes `IsMarkedForDelete` items, prunes after save, exposes `GetItems()`).
- **DomainModels/** — `DomainModelBase` (Id only); `CoreFieldsDomainModelBase` (audit fields +
  `Timestamp` that cross the adapter boundary).
- **Adapters/** — `AdapterBase<TModel, TEntity>`: single-item adapt both directions, collection
  merge (match by Id, add new, mark missing for delete), `BeforeAdapt`/`AfterAdapt` hooks, and an
  `AdapterAction` enum (Adapt/Skip/Delete). Subclasses implement `PerformAdapt`.
- **Repositories/** — `EfCoreRepositoryBase<TEntity, TDbContext>` (owns the DbContext,
  `IDisposable`, `VerifyItemIsAddedOrAttached` for add-vs-attach by `Id == 0`); and
  `EfCoreCrudRepositoryBase<TEntity, TDbContext>` (async `GetAllAsync`/`GetByIdAsync`/
  `SaveAsync`/`DeleteAsync`, implements `IAsyncReadableRepository<TEntity,int>`). The save lifecycle
  runs `BeforeSave` → dependent-collection `BeforeSave` → `SaveChangesAsync` → dependent-collection
  `AfterSave` → `AfterSave`. Override `AddIncludes`/`AddDefaultSort` for eager-loading and sorting.
  (These used only core EF Core APIs, so they are provider-agnostic; the old `SqlEntityFramework*`
  names were renamed during the package split.)
- **ServiceLayers/** — `IValidatorStrategy<T>`, `DefaultValidatorStrategy<T>` (DataAnnotations),
  `IUsernameProvider`; `ServiceLayerBase<TModel, TEntity>` (validate → get/create → adapt → save →
  copy back Id, implements `IAsyncService<TModel,int>`); `CoreFieldsServiceLayerBase<TModel, TEntity>`
  (populates CreatedBy/Date on insert, LastModifiedBy/Date on every save, copies audit fields +
  Timestamp back after save); `InvalidObjectException` / `UnknownObjectException`.
- **Registration/** — `EfCoreRegistrationHelper<TDbContext>` + `services.AddBendayEfCore<TDbContext>(...)`.
  Methods: `ConfigureDbContext`, `RegisterDbContext`, `RegisterRepository`, `RegisterAdapter`,
  `RegisterService`, `RegisterValidator`, `RegisterDefaultValidator`, `RegisterUsernameProvider`, and
  `RegisterAggregate` (repo + adapter + default validator + service in one call — note it does NOT
  register an `IUsernameProvider`, so add `RegisterUsernameProvider` for `CoreFields` services).
  `RegisterDbContext` now requires `ConfigureDbContext(...)` (or the SQL Server `UseConnectionString`
  extension) to have been called first — there is no built-in provider fallback.
- **Migrations/** — `DefaultDesignTimeDbContextFactory<TContext>`: provider-agnostic
  `IDesignTimeDbContextFactory` base that loads a named connection string (default `"default"`) from
  appsettings + environment. Subclasses supply the provider via the abstract `ConfigureProvider`
  seam and construct the context via the abstract `Create(DbContextOptions)`.

### SQL Server layer (`Benday.EfCore.SqlServer`)

Everything provider-specific is isolated here:

- **`UseConnectionString<TDbContext>(connectionString)`** — extension on `EfCoreRegistrationHelper`
  (namespace `Benday.EfCore.Registration`) that calls `ConfigureDbContext(o => o.UseSqlServer(...))`.
- **`ApplyBendaySqlServerConcurrency(this ModelBuilder)`** — call once at the end of
  `OnModelCreating`; maps `CoreFieldsEntityBase.Timestamp` as `rowversion` (`.IsRowVersion()`) for
  every `CoreFieldsEntityBase`-derived entity in the model. This replaces the `[Timestamp]` attribute.
- **`SqlServerDesignTimeDbContextFactory<TContext>`** — subclass of
  `DefaultDesignTimeDbContextFactory<TContext>` that overrides `ConfigureProvider` with `UseSqlServer`.

### The aggregate / dependent-entity lifecycle (load-bearing)

An aggregate root overrides `GetDependentEntities()` to wrap each child collection in a
`DependentEntityCollection<T>`. The repository's `SaveAsync` calls `BeforeSave`/`AfterSave` on each
collection around `SaveChangesAsync` so children marked `IsMarkedForDelete` are deleted from the DB
and pruned from memory. Don't break this ordering.

### Optimistic concurrency gotcha

`CoreFieldsEntityBase.Timestamp` is a rowversion concurrency token. It is **no longer** declared with
`[Timestamp]` (that attribute is provider-specific by convention); the provider-agnostic base type
just exposes a plain `byte[]? Timestamp`. SQL Server consumers map it by calling
`modelBuilder.ApplyBendaySqlServerConcurrency()` at the end of `OnModelCreating` (it applies
`.IsRowVersion()` to every `CoreFieldsEntityBase` entity). This is model-identical to the old
`[Timestamp]` — the swap produces an empty migration. **If you forget the call, you silently lose
optimistic concurrency** (no exception, just no token). Postgres consumers would instead use `xmin`.

Once mapped: a detached `CoreFields` entity **cannot** be blind-attached-and-updated (EF has no
original token → 0 rows affected → `DbUpdateConcurrencyException`). Load the entity first (so it
carries its token), then modify and save — which is what the service layer does. Plain `EntityBase`
entities (no token) can be attach-updated while detached.

## Solution Structure

Solution file: `Benday.EfCore.slnx`. Shipping libraries live under `src/`; test and example
projects live under `test/`.

- `src/Benday.EfCore/` — provider-agnostic library (NuGet package source)
- `src/Benday.EfCore.SqlServer/` — SQL Server wiring library (NuGet package source)
- `src/Benday.EfCore.Testing/` — test doubles package (`InMemoryRepository<T>`, fakes)
- `test/Benday.EfCore.SqlServer.TestApi/` — worked example (Person aggregate: entity, domain model,
  adapter, repository, service, DI wiring) used by the integration tests; references both libraries
- `test/Benday.EfCore.SqlServer.UnitTests/` — xUnit unit tests (in-memory, no DB; references
  `Benday.EfCore.Testing`)
- `test/Benday.EfCore.SqlServer.IntegrationTests/` — xUnit integration tests (require SQL Server)

## Testing Notes

- Test framework is **xUnit (xunit.v3)**, aligned with `Benday.Common.Testing` (`TestClassBase`,
  `AssertThat`, `.ShouldEqual`). Test projects are `OutputType=Exe` with
  `TestingPlatformDotnetTestSupport=true` so `dotnet test` works.
- Integration tests expect SQL Server at `localhost`, database `benday-efcore-sqlserver`, user `sa`,
  password `Pa$$word`; they self-skip when it's unavailable.
- `TestApi` is the reference implementation showing how to build a complete aggregate (repository +
  adapter + `CoreFieldsServiceLayerBase` service) on top of the libraries — including the
  `ApplyBendaySqlServerConcurrency()` call in `TestDbContext.OnModelCreating` and a
  `SqlServerDesignTimeDbContextFactory` subclass.
- Both libraries have XML doc comments on all public types/members (`GenerateDocumentationFile`).

## CI/CD

GitHub Actions workflow: `.github/workflows/benday-efcore.yml`

**Triggers:**
- Push to `main` (with path filters for relevant project directories)
- Pull requests to `main` (same path filters)
- Manual workflow dispatch

**Jobs:**
1. **unit-tests** — builds all three NuGet packages, runs unit tests, packs and uploads artifacts
2. **integration-tests** — runs after unit-tests; uses SQL Server 2025 container, runs integration tests
3. **deploy** — runs after both test jobs pass, only on push to `main`; pushes packages to NuGet.org

**Required secrets:**
- `NUGET_API_KEY` — NuGet.org API key for publishing

**Environment:**
- `nuget-deploy` — optional GitHub environment for approval gates on the deploy job

Pull requests run unit and integration tests but skip the deploy job (validation without publishing).
