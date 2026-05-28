using Benday.Common.Interfaces;
using Benday.EfCore.SqlServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.Repositories;

/// <summary>
/// Base repository. Owns the DbContext and handles the
/// add-vs-attach decision based on Id == 0.
/// </summary>
public abstract class SqlEntityFrameworkRepositoryBase<TEntity, TDbContext> : IDisposable
    where TEntity : EntityBase
    where TDbContext : DbContext
{
    /// <summary>The EF Core DbContext owned by this repository.</summary>
    protected TDbContext Context { get; }

    /// <summary>
    /// Creates the repository over the supplied DbContext.
    /// </summary>
    protected SqlEntityFrameworkRepositoryBase(TDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds new entities (Id == 0) to the DbSet. Attaches existing
    /// entities so EF Core tracks them for update.
    /// </summary>
    protected void VerifyItemIsAddedOrAttached(DbSet<TEntity> dbSet, TEntity entity)
    {
        if (entity.Id == 0)
        {
            dbSet.Add(entity);
        }
        else
        {
            var entry = Context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                dbSet.Attach(entity);
                entry.State = EntityState.Modified;
            }
        }
    }

    /// <summary>
    /// Disposes the owned DbContext.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources (the DbContext) when <paramref name="disposing"/> is true.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Context.Dispose();
        }
    }
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
public abstract class SqlEntityFrameworkCrudRepositoryBase<TEntity, TDbContext>
    : SqlEntityFrameworkRepositoryBase<TEntity, TDbContext>,
      IAsyncReadableRepository<TEntity, int>
    where TEntity : EntityBase
    where TDbContext : DbContext
{
    /// <summary>
    /// Creates the CRUD repository over the supplied DbContext.
    /// </summary>
    protected SqlEntityFrameworkCrudRepositoryBase(TDbContext context) : base(context) { }

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
    public virtual async Task<TEntity?> GetByIdAsync(int id)
    {
        var query = AddIncludes(EntityDbSet.AsQueryable());
        return await query.FirstOrDefaultAsync(e => e.Id == id);
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
