# Benday.EfCore.SqlServer

Base classes for the **repository pattern**, **adapter pattern**, and **service layer pattern**
with Entity Framework Core and SQL Server. Hide your dependencies, keep your domain models clean,
and make your business logic testable without a database.

## About

Written by Benjamin Day
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer
https://www.benday.com
info@benday.com

*Got ideas for features you'd like to see? Found a bug?
Let us know by submitting an [issue](https://github.com/benday-inc/Benday.EfCore.SqlServer/issues)*. *Want to contribute? Submit a pull request.*

[Source code](https://github.com/benday-inc/Benday.EfCore.SqlServer)

[API Documentation](https://benday-inc.github.io/Benday.EfCore.SqlServer/api/index.html)

[NuGet Package](https://www.nuget.org/packages/Benday.EfCore.SqlServer/)

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

> **Upgrading from v10?** This is a breaking release. The search base class
> (`SqlEntityFrameworkSearchableRepositoryBase`) and its predicate methods, the
> `LinqPredicateExtensions`/`ParameterSubstitutionVisitor` helpers, and the dependency on
> `Benday.Common` (`IInt32Identity`) have all been removed. Types now live in feature
> namespaces (`Benday.EfCore.SqlServer.Entities`, `.Repositories`, `.Adapters`, `.ServiceLayers`,
> `.DomainModels`, `.Registration`).

## The layers

| Layer | Base class | Purpose |
|---|---|---|
| **Entity** | `EntityBase`, `CoreFieldsEntityBase` | EF Core entities. `CoreFieldsEntityBase` adds audit fields + a `[Timestamp]` concurrency token. |
| **Domain model** | `DomainModelBase`, `CoreFieldsDomainModelBase` | Business-logic types on the far side of the adapter boundary. EF never sees them. |
| **Adapter** | `AdapterBase<TModel, TEntity>` | Bidirectional mapping, including collection merge (match by Id, add new, mark missing for delete). |
| **Repository** | `SqlEntityFrameworkRepositoryBase`, `SqlEntityFrameworkCrudRepositoryBase` | Async CRUD + the dependent-entity (aggregate) save/delete lifecycle. Implements `IAsyncReadableRepository<T,int>`. |
| **Service** | `ServiceLayerBase`, `CoreFieldsServiceLayerBase` | Orchestration: validate → get/create → adapt → save → copy back. `CoreFields` variant populates audit fields. |
| **Registration** | `EfCoreRegistrationHelper<TDbContext>` | Fluent DI: `services.AddBendayEfCore<MyDbContext>(...)`. |

A companion package, **`Benday.EfCore.SqlServer.Testing`**, provides `InMemoryRepository<T>`,
`FakeValidatorStrategy<T>`, and `FakeUsernameProvider` so you can unit-test your service layer
with no database.

## Aggregate roots and dependent entities

An entity that owns children overrides `GetDependentEntities()` to expose each child collection
as a `DependentEntityCollection<T>`. The repository then handles the child lifecycle on save:
children flagged `IsMarkedForDelete` are removed from the database, and the in-memory collection
is pruned afterward.

## Optimistic concurrency note

`CoreFieldsEntityBase` carries a `[Timestamp]` (rowversion) concurrency token. Because of this,
**you cannot blind-update a detached `CoreFields` entity** — EF has no original rowversion to check
and the update affects zero rows. Always **load the entity first** (so it carries its token), then
modify and save. The service layer does exactly this (`GetByIdAsync` → adapt → save). Plain
`EntityBase` entities (no token) can be attach-updated while detached.

## Build and test

```bash
# Build
dotnet build Benday.EfCore.SqlServer.slnx

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
