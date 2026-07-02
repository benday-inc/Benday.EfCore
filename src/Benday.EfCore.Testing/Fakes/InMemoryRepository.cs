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
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TIdentity">The primary key type.</typeparam>
public class InMemoryRepository<T, TIdentity> : IAsyncReadableRepository<T, TIdentity>
    where T : EntityBase<TIdentity>
    where TIdentity : IEquatable<TIdentity>
{
    private int _nextIdCounter = 1;

    /// <summary>The backing store of saved entities.</summary>
    public List<T> Items { get; set; } = new();

    // --- Method call tracking ---

    /// <summary>True once <see cref="GetAllAsync"/> has been called.</summary>
    public bool WasGetAllCalled { get; private set; }

    /// <summary>True once <see cref="GetByIdAsync"/> has been called.</summary>
    public bool WasGetByIdCalled { get; private set; }

    /// <summary>The id passed to the most recent <see cref="GetByIdAsync"/> call.</summary>
    public TIdentity? GetByIdArgumentValue { get; private set; }

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
        GetByIdArgumentValue = default;
        WasSaveCalled = false;
        SaveArgumentValue = default;
        WasDeleteCalled = false;
        DeleteArgumentValue = default;
    }

    // --- Identity seams ---

    /// <summary>
    /// Returns true when the identity value indicates a new, unpersisted item.
    /// Default: Id equals default(TIdentity). Override for client-assigned keys.
    /// </summary>
    protected virtual bool IsNew(TIdentity id) =>
        EqualityComparer<TIdentity>.Default.Equals(id, default!);

    /// <summary>
    /// Produces deterministic sequential identity values for testing:
    /// int → 1, 2, 3; Guid → 00000000-0000-0000-0000-000000000001, etc.;
    /// string → "1", "2", "3". Override for custom key strategies.
    /// </summary>
    protected virtual TIdentity GenerateId()
    {
        var id = _nextIdCounter++;

        if (typeof(TIdentity) == typeof(int))
            return (TIdentity)(object)id;

        if (typeof(TIdentity) == typeof(Guid))
            return (TIdentity)(object)Guid.Parse(
                $"00000000-0000-0000-0000-{id:D12}");

        if (typeof(TIdentity) == typeof(string))
            return (TIdentity)(object)id.ToString();

        throw new NotSupportedException(
            $"Override GenerateId() for key type {typeof(TIdentity).Name}.");
    }

    // --- IAsyncReadableRepository implementation ---

    /// <inheritdoc />
    public Task<IList<T>> GetAllAsync()
    {
        WasGetAllCalled = true;
        return Task.FromResult<IList<T>>(Items.ToList());
    }

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(TIdentity id)
    {
        WasGetByIdCalled = true;
        GetByIdArgumentValue = id;

        var match = Items.FirstOrDefault(item => item.Id.Equals(id));
        return Task.FromResult(match);
    }

    /// <inheritdoc />
    public Task SaveAsync(T entity)
    {
        WasSaveCalled = true;
        SaveArgumentValue = entity;

        if (IsNew(entity.Id))
        {
            entity.Id = GenerateId();
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
    /// in-memory collection, and new items (see <see cref="IsNew"/>) are assigned identities.
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

            // Only the generic collection exposes typed identity access, which
            // is what we need to assign ids the way EF Core would.
            if (collection is IDependentEntityCollection<TIdentity> typedCollection)
            {
                foreach (var item in typedCollection.GetItems())
                {
                    if (IsNew(item.Id))
                    {
                        item.Id = GenerateId();
                    }

                    if (item is IEntityWithDependents nested)
                    {
                        ProcessDependents(nested);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Non-generic int convenience shim over <see cref="InMemoryRepository{T, TIdentity}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class InMemoryRepository<T> : InMemoryRepository<T, int>
    where T : EntityBase<int>
{
}
