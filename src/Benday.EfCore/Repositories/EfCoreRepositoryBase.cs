using Benday.EfCore.Entities;

using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.Repositories;

/// <summary>
/// Non-generic int convenience shim over
/// <see cref="EfCoreRepositoryBase{TEntity, TIdentity, TDbContext}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TDbContext">The DbContext type.</typeparam>
public abstract class EfCoreRepositoryBase<TEntity, TDbContext>
    : EfCoreRepositoryBase<TEntity, int, TDbContext>
    where TEntity : EntityBase<int>
    where TDbContext : DbContext
{
    /// <summary>
    /// Creates the repository over the supplied DbContext.
    /// </summary>
    protected EfCoreRepositoryBase(TDbContext context) : base(context) { }
}

/// <summary>
/// Base repository. Owns the DbContext and handles the
/// add-vs-attach decision based on the <see cref="IsNew"/> seam.
///
/// The identity type is generic (<typeparamref name="TIdentity"/>). Int consumers
/// use the non-generic <see cref="EfCoreRepositoryBase{TEntity, TDbContext}"/> shim.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TIdentity">The primary key type.</typeparam>
/// <typeparam name="TDbContext">The DbContext type.</typeparam>
public abstract class EfCoreRepositoryBase<TEntity, TIdentity, TDbContext> : IDisposable
    where TEntity : EntityBase<TIdentity>
    where TIdentity : IEquatable<TIdentity>
    where TDbContext : DbContext
{
    /// <summary>The EF Core DbContext owned by this repository.</summary>
    protected TDbContext Context { get; }

    /// <summary>
    /// Creates the repository over the supplied DbContext.
    /// </summary>
    protected EfCoreRepositoryBase(TDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Returns true when the entity is new (unpersisted) and should be added
    /// rather than attached. Default: Id equals default(TIdentity) — 0 for int,
    /// Guid.Empty for Guid, null for string. Override for client-assigned keys.
    /// </summary>
    protected virtual bool IsNew(TEntity entity) =>
        EqualityComparer<TIdentity>.Default.Equals(entity.Id, default!);

    /// <summary>
    /// Adds new entities to the DbSet. Attaches existing
    /// entities so EF Core tracks them for update.
    /// </summary>
    protected void VerifyItemIsAddedOrAttached(DbSet<TEntity> dbSet, TEntity entity)
    {
        if (IsNew(entity))
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



