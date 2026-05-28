using Benday.Common.Interfaces;

namespace Benday.EfCore.SqlServer.Adapters;

/// <summary>
/// Controls what happens to an individual item during adaptation.
/// Returned from BeforeAdapt hooks.
/// </summary>
public enum AdapterAction
{
    /// <summary>Proceed with the normal adapt/copy.</summary>
    Adapt,
    /// <summary>Skip this item — don't copy, don't add.</summary>
    Skip,
    /// <summary>Mark this item for deletion.</summary>
    Delete
}

/// <summary>
/// Bidirectional adapter between a domain model (TModel) and
/// an EF Core entity (TEntity).
///
/// Handles single-item mapping in both directions, plus the tricky
/// part: collection mapping with merge logic. When adapting a list of
/// models to a list of entities, the adapter:
///
/// - Matches existing items by Id and updates them in place
/// - Creates new entity instances for models with Id == 0
/// - Marks entities for delete when their Id no longer appears
///   in the model list (via IsMarkedForDelete)
///
/// Subclasses implement PerformAdapt for the actual property copying.
/// The base class handles the lifecycle, matching, and delete detection.
/// </summary>
public abstract class AdapterBase<TModel, TEntity>
    where TModel : class, IEntityIdentity<int>, new()
    where TEntity : class, IEntityIdentity<int>, IDeleteable, new()
{
    // --- Single-item mapping ---

    /// <summary>
    /// Adapt a model to an entity.
    /// </summary>
    public void Adapt(TModel fromValue, TEntity toValue)
    {
        if (fromValue == null) throw new ArgumentNullException(nameof(fromValue));
        if (toValue == null) throw new ArgumentNullException(nameof(toValue));

        var action = BeforeAdapt(fromValue, toValue);
        if (action == AdapterAction.Skip) return;
        if (action == AdapterAction.Delete)
        {
            toValue.IsMarkedForDelete = true;
            return;
        }

        PerformAdapt(fromValue, toValue);
        AfterAdapt(fromValue, toValue);
    }

    /// <summary>
    /// Adapt an entity to a model.
    /// </summary>
    public void Adapt(TEntity fromValue, TModel toValue)
    {
        if (fromValue == null) throw new ArgumentNullException(nameof(fromValue));
        if (toValue == null) throw new ArgumentNullException(nameof(toValue));

        var action = BeforeAdapt(fromValue, toValue);
        if (action == AdapterAction.Skip) return;

        PerformAdapt(fromValue, toValue);
        AfterAdapt(fromValue, toValue);
    }

    // --- Collection mapping ---

    /// <summary>
    /// Adapt a list of models to a list of entities with merge logic.
    /// New items are added. Existing items are updated by Id match.
    /// Items missing from the model list are marked for delete.
    /// </summary>
    public void Adapt(IList<TModel> fromValues, IList<TEntity> toValues)
    {
        if (fromValues == null) throw new ArgumentNullException(nameof(fromValues));
        if (toValues == null) throw new ArgumentNullException(nameof(toValues));

        foreach (var fromValue in fromValues)
        {
            TEntity? toValue;
            bool isNew = false;

            if (fromValue.Id == 0)
            {
                toValue = new TEntity();
                isNew = true;
            }
            else
            {
                toValue = FindById(toValues, fromValue.Id);
                if (toValue == null)
                {
                    toValue = new TEntity();
                    isNew = true;
                }
            }

            var action = BeforeAdapt(fromValue, toValue);

            if (action == AdapterAction.Adapt)
            {
                PerformAdapt(fromValue, toValue);
                AfterAdapt(fromValue, toValue);

                if (isNew)
                {
                    toValues.Add(toValue);
                }
            }
            else if (action == AdapterAction.Delete)
            {
                toValue.IsMarkedForDelete = true;
            }
        }

        // Mark entities for delete when their Id no longer appears
        // in the model list.
        MarkDeletedItems(fromValues, toValues);
    }

    /// <summary>
    /// Adapt a list of entities to a list of models.
    /// </summary>
    public void Adapt(IList<TEntity> fromValues, IList<TModel> toValues)
    {
        if (fromValues == null) throw new ArgumentNullException(nameof(fromValues));
        if (toValues == null) throw new ArgumentNullException(nameof(toValues));

        foreach (var fromValue in fromValues)
        {
            var toValue = new TModel();

            var action = BeforeAdapt(fromValue, toValue);

            if (action == AdapterAction.Adapt)
            {
                PerformAdapt(fromValue, toValue);
                AfterAdapt(fromValue, toValue);
                toValues.Add(toValue);
            }
        }
    }

    // --- Abstract: subclasses provide property mapping ---

    /// <summary>
    /// Map properties from a model to an entity.
    /// Implement the actual property-by-property copy here.
    /// </summary>
    protected abstract void PerformAdapt(TModel fromValue, TEntity toValue);

    /// <summary>
    /// Map properties from an entity to a model.
    /// Implement the actual property-by-property copy here.
    /// </summary>
    protected abstract void PerformAdapt(TEntity fromValue, TModel toValue);

    // --- Hooks ---

    /// <summary>
    /// Hook invoked before a model is adapted to an entity. Return an
    /// <see cref="AdapterAction"/> to control whether the item is adapted,
    /// skipped, or marked for delete. Defaults to Adapt.
    /// </summary>
    protected virtual AdapterAction BeforeAdapt(TModel fromValue, TEntity toValue)
        => AdapterAction.Adapt;

    /// <summary>
    /// Hook invoked before an entity is adapted to a model. Return an
    /// <see cref="AdapterAction"/> to control the outcome. Defaults to Adapt.
    /// </summary>
    protected virtual AdapterAction BeforeAdapt(TEntity fromValue, TModel toValue)
        => AdapterAction.Adapt;

    /// <summary>Hook invoked after a model has been adapted to an entity.</summary>
    protected virtual void AfterAdapt(TModel fromValue, TEntity toValue) { }

    /// <summary>Hook invoked after an entity has been adapted to a model.</summary>
    protected virtual void AfterAdapt(TEntity fromValue, TModel toValue) { }

    // --- Private helpers ---

    private static TEntity? FindById(IList<TEntity> items, int id)
        => items.FirstOrDefault(item => item.Id == id);

    private static void MarkDeletedItems(IList<TModel> fromValues, IList<TEntity> toValues)
    {
        var modelIds = new HashSet<int>(fromValues.Select(m => m.Id).Where(id => id != 0));

        foreach (var entity in toValues)
        {
            if (entity.Id != 0 && !modelIds.Contains(entity.Id))
            {
                entity.IsMarkedForDelete = true;
            }
        }
    }
}
