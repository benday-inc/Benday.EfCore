using System.Runtime.CompilerServices;
using Benday.Common.Interfaces;
using Benday.EfCore.Diagnostics;
using Benday.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.Repositories;

/// <summary>
/// Non-generic int convenience shim over
/// <see cref="EfCoreCrudRepositoryBase{TEntity, TIdentity, TDbContext}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TDbContext">The DbContext type.</typeparam>
public abstract class EfCoreCrudRepositoryBase<TEntity, TDbContext>
    : EfCoreCrudRepositoryBase<TEntity, int, TDbContext>
    where TEntity : EntityBase<int>
    where TDbContext : DbContext
{
    /// <summary>
    /// Creates the CRUD repository over the supplied DbContext.
    /// </summary>
    protected EfCoreCrudRepositoryBase(TDbContext context) : base(context) { }
}

/// <summary>
/// CRUD repository with async operations and aggregate root support.
///
/// Implements IAsyncReadableRepository from Benday.Common.Interfaces so
/// the same repository contract works whether the storage is SQL Server,
/// Cosmos DB, or anything else.
///
/// The save lifecycle handles dependent entities automatically:
/// 1. VerifyItemIsAddedOrAttached
/// 2. BeforeSave (override for custom logic)
/// 3. BeforeSave on each DependentEntityCollection (marks-for-delete handling)
/// 4. SaveChangesAsync
/// 5. AfterSave on each DependentEntityCollection (prune in-memory)
/// 6. AfterSave (override for custom logic)
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TIdentity">The primary key type.</typeparam>
/// <typeparam name="TDbContext">The DbContext type.</typeparam>
public abstract class EfCoreCrudRepositoryBase<TEntity, TIdentity, TDbContext>
    : EfCoreRepositoryBase<TEntity, TIdentity, TDbContext>,
      IAsyncReadableRepository<TEntity, TIdentity>
    where TEntity : EntityBase<TIdentity>
    where TIdentity : IEquatable<TIdentity>
    where TDbContext : DbContext
{
    /// <summary>
    /// Creates the CRUD repository over the supplied DbContext.
    /// </summary>
    protected EfCoreCrudRepositoryBase(TDbContext context) : base(context) { }

    /// <summary>
    /// The DbSet for this entity type. Subclasses must provide this.
    /// </summary>
    protected abstract DbSet<TEntity> EntityDbSet { get; }

    /// <summary>
    /// Override to add .Include() calls for eager loading.
    /// Default returns the queryable unchanged.
    /// </summary>
    protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> queryable) => queryable;

    /// <summary>
    /// Override to add a default sort order.
    /// Default returns the queryable unchanged.
    /// </summary>
    protected virtual IQueryable<TEntity> AddDefaultSort(IQueryable<TEntity> queryable) => queryable;

    /// <summary>
    /// The diagnostics source description for this repository, used as the
    /// prefix for query tags and correlation scopes (e.g. the
    /// "PersonRepository" in "PersonRepository.GetByIdAsync"). Defaults to the
    /// concrete repository type name; override to customize.
    /// </summary>
    protected virtual string DiagnosticsSourceName => GetType().Name;

    /// <summary>
    /// Tags a query for diagnostics as "<c>&lt;DiagnosticsSourceName&gt;.&lt;caller&gt;</c>"
    /// — the calling method name is captured automatically, so custom query
    /// methods get the same attribution as the built-in CRUD methods with no
    /// hand-written strings. The tag rides in the SQL (visible in Query Store /
    /// the diagnostics <c>Tags</c>) and the interceptor also uses it to populate
    /// the diagnostics <c>Source</c> when no correlation scope is active.
    ///
    /// <para>
    /// The tag is a constant per (type, method), so it does not affect SQL
    /// Server plan-cache reuse. Never pass runtime values.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">The query element type.</typeparam>
    /// <param name="query">The query to tag.</param>
    /// <param name="operationName">
    /// Captured automatically from the caller; do not pass explicitly except to
    /// override the label.
    /// </param>
    protected IQueryable<TResult> Tag<TResult>(
        IQueryable<TResult> query,
        [CallerMemberName] string operationName = "")
        => query.TagWith($"{DiagnosticsSourceName}.{operationName}");

    /// <summary>
    /// Opens an ambient diagnostics correlation scope named
    /// "<c>&lt;DiagnosticsSourceName&gt;.&lt;caller&gt;</c>" so that commands which
    /// cannot carry a <c>TagWith</c> tag — the INSERT/UPDATE/DELETE emitted by
    /// SaveChanges — are attributed to the calling operation. Dispose (via a
    /// <c>using</c>) to restore the previous scope. Use this in custom methods
    /// that call <c>SaveChanges</c> outside the built-in Save/Delete path.
    /// </summary>
    /// <param name="operationName">
    /// Captured automatically from the caller; do not pass explicitly except to
    /// override the label.
    /// </param>
    protected IDisposable DiagnosticsScope([CallerMemberName] string operationName = "")
        => EfCoreDiagnosticsCorrelation.Push($"{DiagnosticsSourceName}.{operationName}");

    /// <summary>
    /// Returns all entities, applying includes and the default sort.
    /// </summary>
    public virtual async Task<IList<TEntity>> GetAllAsync()
    {
        var query = Tag(AddDefaultSort(AddIncludes(EntityDbSet.AsQueryable())));
        return await query.ToListAsync();
    }

    /// <summary>
    /// Returns the entity with the supplied id, applying includes, or null.
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(TIdentity id)
    {
        var query = Tag(AddIncludes(EntityDbSet.AsQueryable()));
        // .Equals() instead of == because TIdentity is generic; for int/Guid/string
        // this translates to SQL correctly.
        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id));
    }

    /// <summary>
    /// Saves the entity, running the dependent-entity lifecycle around SaveChanges.
    /// </summary>
    public virtual async Task SaveAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        VerifyItemIsAddedOrAttached(EntityDbSet, entity);

        BeforeSave(entity);

        HandleDependentEntitiesBeforeSave(entity);

        // SaveChanges emits INSERT/UPDATE/DELETE, which TagWith can't reach
        // (it only rides on IQueryable). Attribute the write path via the
        // ambient correlation scope instead.
        using (DiagnosticsScope())
        {
            await Context.SaveChangesAsync();
        }

        HandleDependentEntitiesAfterSave(entity);

        AfterSave(entity);
    }

    /// <summary>
    /// Deletes the entity from the database.
    /// </summary>
    public virtual async Task DeleteAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        BeforeDelete(entity);

        EntityDbSet.Remove(entity);

        using (DiagnosticsScope())
        {
            await Context.SaveChangesAsync();
        }

        AfterDelete(entity);
    }

    /// <summary>Override for custom logic before save.</summary>
    protected virtual void BeforeSave(TEntity entity) { }

    /// <summary>Override for custom logic after save.</summary>
    protected virtual void AfterSave(TEntity entity) { }

    /// <summary>Override for custom logic before delete.</summary>
    protected virtual void BeforeDelete(TEntity entity) { }

    /// <summary>Override for custom logic after delete.</summary>
    protected virtual void AfterDelete(TEntity entity) { }

    private void HandleDependentEntitiesBeforeSave(TEntity entity)
    {
        var dependents = entity.GetDependentEntities();
        if (dependents == null) return;

        foreach (var collection in dependents)
        {
            collection.BeforeSave(Context);
        }
    }

    private void HandleDependentEntitiesAfterSave(TEntity entity)
    {
        var dependents = entity.GetDependentEntities();
        if (dependents == null) return;

        foreach (var collection in dependents)
        {
            collection.AfterSave();
        }
    }
}
