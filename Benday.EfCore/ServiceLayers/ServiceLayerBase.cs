using Benday.Common.Interfaces;
using Benday.EfCore.Adapters;
using Benday.EfCore.Entities;

namespace Benday.EfCore.ServiceLayers;

/// <summary>
/// Invalid object exception thrown when validation fails in the service layer.
/// </summary>
public class InvalidObjectException : InvalidOperationException
{
    /// <summary>
    /// Creates an exception describing an invalid object.
    /// </summary>
    public InvalidObjectException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when an entity cannot be found by Id.
/// </summary>
public class UnknownObjectException : InvalidOperationException
{
    /// <summary>
    /// Creates an exception describing an object that could not be located.
    /// </summary>
    public UnknownObjectException(string typeName, int id)
        : base($"Could not locate a {typeName} with an id of '{id}'.") { }
}

/// <summary>
/// Base service layer. Handles the core flow:
/// validate → get or create entity → adapt → save → copy back IDs.
///
/// The service owns the adapter. The repository only deals in entities.
/// The controller/API layer only deals in domain models. The service
/// is the bridge.
/// </summary>
public abstract class ServiceLayerBase<TModel, TEntity>
    : IAsyncService<TModel, int>
    where TModel : class, IEntityIdentity<int>, new()
    where TEntity : EntityBase, new()
{
    /// <summary>The repository used to persist entities.</summary>
    protected IAsyncReadableRepository<TEntity, int> Repository { get; }

    /// <summary>The adapter that maps between models and entities.</summary>
    protected AdapterBase<TModel, TEntity> Adapter { get; }

    /// <summary>The validator applied to models before save.</summary>
    protected IValidatorStrategy<TModel> Validator { get; }

    /// <summary>
    /// Creates the service layer with its repository, adapter, and validator.
    /// </summary>
    protected ServiceLayerBase(
        IAsyncReadableRepository<TEntity, int> repository,
        AdapterBase<TModel, TEntity> adapter,
        IValidatorStrategy<TModel> validator)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        Validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// The type name used in error messages when an entity is not found.
    /// Defaults to typeof(TModel).Name.
    /// </summary>
    protected virtual string EntityTypeName => typeof(TModel).Name;

    /// <summary>
    /// Validates and saves a domain model: validate → get or create the
    /// entity → adapt → save → copy database-assigned fields back to the model.
    /// </summary>
    public virtual async Task SaveAsync(TModel saveThis)
    {
        if (saveThis == null)
            throw new ArgumentNullException(nameof(saveThis));

        if (!Validator.IsValid(saveThis))
            throw new InvalidObjectException($"{EntityTypeName} is invalid.");

        BeforeSave(saveThis);

        TEntity toValue;

        if (saveThis.Id == 0)
        {
            toValue = new TEntity();
        }
        else
        {
            var existing = await Repository.GetByIdAsync(saveThis.Id);
            toValue = existing
                ?? throw new UnknownObjectException(EntityTypeName, saveThis.Id);
        }

        OnBeforeAdapt(saveThis, toValue);

        Adapter.Adapt(saveThis, toValue);

        await Repository.SaveAsync(toValue);

        // Copy back the database-assigned Id and any other
        // fields that changed during save.
        PopulateFieldsFromEntityAfterSave(toValue, saveThis);

        AfterSave(saveThis);
    }

    /// <summary>
    /// Deletes the entity that corresponds to the supplied model.
    /// </summary>
    public virtual async Task DeleteAsync(TModel deleteThis)
    {
        if (deleteThis == null)
            throw new ArgumentNullException(nameof(deleteThis));

        var entity = await Repository.GetByIdAsync(deleteThis.Id)
            ?? throw new UnknownObjectException(EntityTypeName, deleteThis.Id);

        await Repository.DeleteAsync(entity);
    }

    /// <summary>
    /// Deletes the entity with the supplied id.
    /// </summary>
    public virtual async Task DeleteByIdAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id)
            ?? throw new UnknownObjectException(EntityTypeName, id);

        await Repository.DeleteAsync(entity);
    }

    /// <summary>
    /// Loads an entity by id and adapts it to a domain model, or returns null.
    /// </summary>
    public virtual async Task<TModel?> GetByIdAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id);

        if (entity == null) return null;

        var model = new TModel();
        Adapter.Adapt(entity, model);

        BeforeReturnFromGet(model);

        return model;
    }

    /// <summary>
    /// Loads all entities and adapts them to domain models.
    /// </summary>
    public virtual async Task<IList<TModel>> GetAllAsync()
    {
        var entities = await Repository.GetAllAsync();

        var models = new List<TModel>();
        Adapter.Adapt(entities, models);

        foreach (var model in models)
        {
            BeforeReturnFromGet(model);
        }

        return models;
    }

    /// <summary>
    /// Copies the database-assigned Id back to the model after save.
    /// Override to copy additional fields (e.g. audit fields, timestamps).
    /// </summary>
    protected virtual void PopulateFieldsFromEntityAfterSave(
        TEntity fromEntity, TModel toModel)
    {
        toModel.Id = fromEntity.Id;
    }

    // --- Hooks ---

    /// <summary>Override for custom logic before save (after validation has passed).</summary>
    protected virtual void BeforeSave(TModel saveThis) { }

    /// <summary>Override for custom logic after save.</summary>
    protected virtual void AfterSave(TModel saveThis) { }

    /// <summary>Override for custom logic after fetching the entity, before adapting.</summary>
    protected virtual void OnBeforeAdapt(TModel model, TEntity entity) { }

    /// <summary>Override to enrich models after loading from repository.</summary>
    protected virtual void BeforeReturnFromGet(TModel model) { }
}
