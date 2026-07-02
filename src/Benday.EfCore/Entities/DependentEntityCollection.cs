using Benday.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.Entities;

/// <summary>
/// Manages the lifecycle of dependent/child entities during save operations.
/// Before save: removes items marked for delete from the DbSet.
/// After save: prunes the in-memory collection.
/// </summary>
public interface IDependentEntityCollection
{
    /// <summary>
    /// Removes items marked for delete from the context so EF Core will
    /// delete them from the database during SaveChanges.
    /// </summary>
    void BeforeSave(DbContext context);

    /// <summary>
    /// Prunes items marked for delete from the in-memory collection after save.
    /// </summary>
    void AfterSave();
}

/// <summary>
/// Generic extension of <see cref="IDependentEntityCollection"/> that exposes
/// typed identity access for callers (notably test doubles) that need to walk
/// the children and assign identities the way EF Core would during SaveChanges.
/// </summary>
/// <typeparam name="TIdentity">The child entity's primary key type.</typeparam>
public interface IDependentEntityCollection<TIdentity> : IDependentEntityCollection
    where TIdentity : IEquatable<TIdentity>
{
    /// <summary>
    /// Returns the child items in this collection as identity instances.
    /// </summary>
    IEnumerable<IEntityIdentity<TIdentity>> GetItems();
}

/// <summary>
/// Manages the save/delete lifecycle for a collection of child entities.
///
/// Before SaveChanges: any item with IsMarkedForDelete == true is removed
/// from the DbSet so EF Core will delete it from the database.
///
/// After SaveChanges: items marked for delete are pruned from the
/// in-memory collection so the parent entity's navigation property
/// reflects the current state.
/// </summary>
/// <typeparam name="T">The child entity type.</typeparam>
/// <typeparam name="TIdentity">The child entity's primary key type.</typeparam>
public class DependentEntityCollection<T, TIdentity> : IDependentEntityCollection<TIdentity>
    where T : class, IEntityIdentity<TIdentity>, IDeleteable
    where TIdentity : IEquatable<TIdentity>
{
    private readonly IList<T> _items;

    /// <summary>
    /// Creates a dependent entity collection wrapper over a child navigation collection.
    /// </summary>
    /// <param name="items">The child collection owned by the parent entity.</param>
    public DependentEntityCollection(IList<T> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <inheritdoc />
    public void BeforeSave(DbContext context)
    {
        var itemsToRemove = _items
            .Where(item => item.IsMarkedForDelete)
            .ToList();

        foreach (var item in itemsToRemove)
        {
            _items.Remove(item);
            context.Remove(item);
        }
    }

    /// <inheritdoc />
    public void AfterSave()
    {
        var itemsToRemove = _items
            .Where(item => item.IsMarkedForDelete)
            .ToList();

        foreach (var item in itemsToRemove)
        {
            _items.Remove(item);
        }
    }

    /// <inheritdoc />
    public IEnumerable<IEntityIdentity<TIdentity>> GetItems() => _items;
}

/// <summary>
/// Non-generic int convenience shim over <see cref="DependentEntityCollection{T, TIdentity}"/>.
/// Int consumers wrap their child collections with this and keep their existing
/// syntax unchanged.
/// </summary>
/// <typeparam name="T">The child entity type.</typeparam>
public class DependentEntityCollection<T> : DependentEntityCollection<T, int>
    where T : class, IEntityIdentity<int>, IDeleteable
{
    /// <summary>
    /// Creates a dependent entity collection wrapper over a child navigation collection.
    /// </summary>
    /// <param name="items">The child collection owned by the parent entity.</param>
    public DependentEntityCollection(IList<T> items) : base(items) { }
}
