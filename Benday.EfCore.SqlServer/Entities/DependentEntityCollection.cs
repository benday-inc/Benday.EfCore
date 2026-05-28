using Benday.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.Entities;

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
/// Manages the save/delete lifecycle for a collection of child entities.
///
/// Before SaveChanges: any item with IsMarkedForDelete == true is removed
/// from the DbSet so EF Core will delete it from the database.
///
/// After SaveChanges: items marked for delete are pruned from the
/// in-memory collection so the parent entity's navigation property
/// reflects the current state.
/// </summary>
public class DependentEntityCollection<T> : IDependentEntityCollection
    where T : class, IEntityIdentity<int>, IDeleteable
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
}
