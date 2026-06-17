using Benday.Common.Interfaces;
using Benday.EfCore.Entities;

namespace Benday.EfCore.Testing.Fakes;

/// <summary>
/// In-memory repository for unit testing. Implements the same
/// IAsyncReadableRepository interface as the real SQL repository,
/// so your service layer tests don't know the difference.
///
/// Also tracks which methods were called, so you can assert that
/// your service layer is interacting with the repository correctly.
///
/// This is the thing that makes "testing without a database" work.
/// Your service layer talks to this fake. Your tests run in milliseconds.
/// No connection string. No SQL Server. No shared dev database.
/// </summary>
public class InMemoryRepository<T> : IAsyncReadableRepository<T, int>
    where T : EntityBase
{
    private int _nextId = 1;

    /// <summary>The backing store of saved entities.</summary>
    public List<T> Items { get; set; } = new();

    // --- Method call tracking ---

    /// <summary>True once <see cref="GetAllAsync"/> has been called.</summary>
    public bool WasGetAllCalled { get; private set; }

    /// <summary>True once <see cref="GetByIdAsync"/> has been called.</summary>
    public bool WasGetByIdCalled { get; private set; }

    /// <summary>The id passed to the most recent <see cref="GetByIdAsync"/> call.</summary>
    public int GetByIdArgumentValue { get; private set; }

    /// <summary>True once <see cref="SaveAsync"/> has been called.</summary>
    public bool WasSaveCalled { get; private set; }

    /// <summary>The entity passed to the most recent <see cref="SaveAsync"/> call.</summary>
    public T? SaveArgumentValue { get; private set; }

    /// <summary>True once <see cref="DeleteAsync"/> has been called.</summary>
    public bool WasDeleteCalled { get; private set; }

    /// <summary>The entity passed to the most recent <see cref="DeleteAsync"/> call.</summary>
    public T? DeleteArgumentValue { get; private set; }

    /// <summary>Resets all method-call tracking flags and captured arguments.</summary>
    public void ResetMethodCallTrackers()
    {
        WasGetAllCalled = false;
        WasGetByIdCalled = false;
        GetByIdArgumentValue = 0;
        WasSaveCalled = false;
        SaveArgumentValue = default;
        WasDeleteCalled = false;
        DeleteArgumentValue = default;
    }

    // --- IAsyncReadableRepository implementation ---

    /// <inheritdoc />
    public Task<IList<T>> GetAllAsync()
    {
        WasGetAllCalled = true;
        return Task.FromResult<IList<T>>(Items.ToList());
    }

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(int id)
    {
        WasGetByIdCalled = true;
        GetByIdArgumentValue = id;

        var match = Items.FirstOrDefault(item => item.Id == id);
        return Task.FromResult(match);
    }

    /// <inheritdoc />
    public Task SaveAsync(T entity)
    {
        WasSaveCalled = true;
        SaveArgumentValue = entity;

        if (entity.Id == 0)
        {
            entity.Id = _nextId++;
        }

        if (!Items.Contains(entity))
        {
            Items.Add(entity);
        }

        // Mirror EF Core's SaveChanges for the aggregate: prune children
        // flagged for delete, then assign identities to new children.
        ProcessDependents(entity);

        OnSave(entity);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(T entity)
    {
        WasDeleteCalled = true;
        DeleteArgumentValue = entity;

        Items.Remove(entity);
        OnDelete(entity);

        return Task.CompletedTask;
    }

    // --- Hooks for subclasses ---

    /// <summary>Override for custom logic after in-memory save.</summary>
    protected virtual void OnSave(T entity) { }

    /// <summary>Override for custom logic after in-memory delete.</summary>
    protected virtual void OnDelete(T entity) { }

    // --- Helpers ---

    /// <summary>
    /// Walks the aggregate's dependent collections, mirroring what EF Core
    /// does during SaveChanges: items marked for delete are pruned from the
    /// in-memory collection, and new items (Id == 0) are assigned identities.
    /// Recurses into nested aggregate roots.
    /// </summary>
    private void ProcessDependents(IEntityWithDependents parent)
    {
        var dependents = parent.GetDependentEntities();
        if (dependents == null) return;

        foreach (var collection in dependents)
        {
            // Remove children flagged for delete (same as the real repo's AfterSave).
            collection.AfterSave();

            foreach (var item in collection.GetItems())
            {
                if (item.Id == 0)
                {
                    item.Id = _nextId++;
                }

                if (item is IEntityWithDependents nested)
                {
                    ProcessDependents(nested);
                }
            }
        }
    }
}
