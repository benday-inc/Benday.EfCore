using Benday.Common.Interfaces;
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
    /// Returns all entities, applying includes and the default sort.
    /// </summary>
    public virtual async Task<IList<TEntity>> GetAllAsync()
    {
        var query = AddDefaultSort(AddIncludes(EntityDbSet.AsQueryable()));
        return await query.ToListAsync();
    }

    /// <summary>
    /// Returns the entity with the supplied id, applying includes, or null.
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(TIdentity id)
    {
        var query = AddIncludes(EntityDbSet.AsQueryable());
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

        await Context.SaveChangesAsync();

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

        await Context.SaveChangesAsync();

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
