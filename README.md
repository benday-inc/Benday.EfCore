# Benday.EfCore

Base classes for the **repository pattern**, **adapter pattern**, and **service layer pattern**
with Entity Framework Core. Hide your dependencies, keep your domain models clean,
and make your business logic testable without a database.

## Packages

| Package | Description |
|---|---|
| [`Benday.EfCore`](https://www.nuget.org/packages/Benday.EfCore/) | Provider-agnostic core: entities, domain models, adapters, repositories, service layers, DI registration |
| [`Benday.EfCore.SqlServer`](https://www.nuget.org/packages/Benday.EfCore.SqlServer/) | SQL Server wiring: `UseConnectionString`, `ApplyBendaySqlServerConcurrency`, design-time factory |
| [`Benday.EfCore.Testing`](https://www.nuget.org/packages/Benday.EfCore.Testing/) | Test doubles: `InMemoryRepository<T>`, `FakeValidatorStrategy<T>`, `FakeUsernameProvider` |

## About

Written by Benjamin Day
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer
https://www.benday.com
info@benday.com

*Got ideas for features you'd like to see? Found a bug?
Let us know by submitting an [issue](https://github.com/benday-inc/Benday.EfCore/issues)*. *Want to contribute? Submit a pull request.*

[Source code](https://github.com/benday-inc/Benday.EfCore)

[API Documentation](https://benday-inc.github.io/Benday.EfCore/api/index.html)

## What's in v11

v11 is a ground-up modernization:

- **.NET 10** and **EF Core 10**.
- **Async throughout** — every repository and service method is async.
- **Shared interface layer** via [`Benday.Common.Interfaces`](https://www.nuget.org/packages/Benday.Common.Interfaces/)
  (`IEntityIdentity<int>`, `IDeleteable`, `IAsyncReadableRepository<T,int>`, `IAsyncService<T,int>`).
  This is the same contract used by [`Benday.CosmosDb`](https://www.nuget.org/packages/Benday.CosmosDb/) —
  swap the storage, keep the contract.
- **Adapter + service layer** base classes for mapping between domain models and EF entities.
- **No search predicate machinery** — write your own LINQ queries in your repository.
- **Opt-in query diagnostics** — slow-query flagging and per-method attribution for dev-time perf
  work (see [Query diagnostics](#query-diagnostics-development-perf-tooling)).

> **Upgrading from v10?** This is a breaking release. The search base class
> (`SqlEntityFrameworkSearchableRepositoryBase`) and its predicate methods, the
> `LinqPredicateExtensions`/`ParameterSubstitutionVisitor` helpers, and the dependency on
> `Benday.Common` (`IInt32Identity`) have all been removed. Types now live in feature
> namespaces (`Benday.EfCore.SqlServer.Entities`, `.Repositories`, `.Adapters`, `.ServiceLayers`,
> `.DomainModels`, `.Registration`).

## The layers

| Layer | Base class | Purpose |
|---|---|---|
| **Entity** | `EntityBase`, `CoreFieldsEntityBase` | EF Core entities. `CoreFieldsEntityBase` adds audit fields + a concurrency token (mapped per provider). |
| **Domain model** | `DomainModelBase`, `CoreFieldsDomainModelBase` | Business-logic types on the far side of the adapter boundary. EF never sees them. |
| **Adapter** | `AdapterBase<TModel, TEntity>` | Bidirectional mapping, including collection merge (match by Id, add new, mark missing for delete). |
| **Repository** | `EfCoreRepositoryBase`, `EfCoreCrudRepositoryBase` | Async CRUD + the dependent-entity (aggregate) save/delete lifecycle. Implements `IAsyncReadableRepository<T,int>`. |
| **Service** | `ServiceLayerBase`, `CoreFieldsServiceLayerBase` | Orchestration: validate → get/create → adapt → save → copy back. `CoreFields` variant populates audit fields. |
| **Registration** | `EfCoreRegistrationHelper<TDbContext>` | Fluent DI: `services.AddBendayEfCore<MyDbContext>(...)`. |

The companion package **`Benday.EfCore.Testing`** provides `InMemoryRepository<T>`,
`FakeValidatorStrategy<T>`, and `FakeUsernameProvider` so you can unit-test your service layer
with no database.

## Aggregate roots and dependent entities

An entity that owns children overrides `GetDependentEntities()` to expose each child collection
as a `DependentEntityCollection<T>`. The repository then handles the child lifecycle on save:
children flagged `IsMarkedForDelete` are removed from the database, and the in-memory collection
is pruned afterward.

## Optimistic concurrency note

`CoreFieldsEntityBase` exposes a `Timestamp` property for optimistic concurrency. The property is
provider-agnostic — SQL Server consumers map it as `rowversion` by calling
`modelBuilder.ApplyBendaySqlServerConcurrency()` in `OnModelCreating`. Because of this token,
**you cannot blind-update a detached `CoreFields` entity** — EF has no original rowversion to check
and the update affects zero rows. Always **load the entity first** (so it carries its token), then
modify and save. The service layer does exactly this (`GetByIdAsync` → adapt → save). Plain
`EntityBase` entities (no token) can be attach-updated while detached.

## Query diagnostics (development perf tooling)

Opt-in diagnostics for finding slow and chatty queries during development — modeled on the diagnostics
in [`Benday.CosmosDb`](https://www.nuget.org/packages/Benday.CosmosDb/). It's **off by default and
zero-overhead** until you turn it on.

Enable it at registration (the interceptor ships in **`Benday.EfCore.SqlServer`**):

```csharp
services.AddBendayEfCore<MyDbContext>(options =>
{
    options.UseConnectionString(connectionString);

    // route every captured query to an NDJSON file...
    options.WithQueryLogSink<MyDbContext, FileEfCoreQueryLogSink>();
    // ...and flag anything slower than 100 ms
    options.WithQueryDiagnostics(o => o.SlowQueryThreshold = TimeSpan.FromMilliseconds(100));

    options.RegisterDbContext();
    // ... RegisterAggregate<...>() etc.
});
```

Each database command produces a structured `EfCoreQueryDiagnostics` event — SQL text, duration, row
count, an `ExceededThreshold` flag, and a `Source` label identifying the repository method that issued
it. Implement `IEfCoreQueryLogSink` for custom handling, or use the built-in `FileEfCoreQueryLogSink`
(NDJSON, written off-thread so it never blocks a query).

Repository methods are labeled automatically. The CRUD base tags its own queries; in a **custom query
method** you get the same labeling from a single `Tag(...)` call:

```csharp
public async Task<IList<Person>> SearchByLastNameAsync(string lastName)
{
    // tagged "SqlPersonRepository.SearchByLastNameAsync" — method name captured automatically
    var query = Tag(AddIncludes(EntityDbSet.AsQueryable())
        .Where(p => p.LastName == lastName));

    return await query.ToListAsync();
}
```

For a custom method that **writes** (calls `SaveChanges`), wrap it in `using (DiagnosticsScope()) { ... }`
instead — inserts/updates/deletes can't carry a query tag. Keep tags constant (type + method); never
interpolate runtime values, or you'll pollute the SQL Server plan cache.

## Build and test

```bash
# Build
dotnet build Benday.EfCore.slnx

# Unit tests (in-memory, no database required)
dotnet test Benday.EfCore.SqlServer.UnitTests

# Integration tests (require SQL Server — see below)
dotnet test Benday.EfCore.SqlServer.IntegrationTests
```

### Local SQL Server for integration tests

The integration tests need SQL Server at `localhost` (`sa` / `Pa$$word`). Start it in Docker:

```powershell
./start-local-dev.ps1          # starts the sql_server container
./start-local-dev.ps1 -Pull    # also pull the latest image
./start-local-dev.ps1 -Remove  # recreate the container from scratch
```

The `benday-efcore-sqlserver` database and its schema are created automatically via EF Core
migrations on first test run. When SQL Server is not reachable, the integration tests **skip**
(rather than fail), so `dotnet test` on the whole solution stays green without a database.

## CI/CD

GitHub Actions runs on every push and pull request to `main`:

1. **Unit tests** — builds all packages, runs unit tests, packs NuGet artifacts
2. **Integration tests** — runs against SQL Server 2025 in a container
3. **Deploy** — pushes to NuGet.org on push to `main` (requires `NUGET_API_KEY` secret)

Pull requests get full validation without publishing.
