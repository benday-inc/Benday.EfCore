using Benday.Common.Interfaces;
using Benday.EfCore.Adapters;
using Benday.EfCore.DomainModels;
using Benday.EfCore.Entities;

namespace Benday.EfCore.ServiceLayers;

/// <summary>
/// Non-generic int convenience shim over
/// <see cref="CoreFieldsServiceLayerBase{TModel, TEntity, TIdentity}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
/// <typeparam name="TModel">The domain model type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public abstract class CoreFieldsServiceLayerBase<TModel, TEntity>
    : CoreFieldsServiceLayerBase<TModel, TEntity, int>
    where TModel : CoreFieldsDomainModelBase<int>, new()
    where TEntity : CoreFieldsEntityBase<int>, new()
{
    /// <summary>
    /// Creates the service layer with its repository, adapter, validator, and username provider.
    /// </summary>
    protected CoreFieldsServiceLayerBase(
        IAsyncReadableRepository<TEntity, int> repository,
        AdapterBase<TModel, TEntity, int> adapter,
        IValidatorStrategy<TModel> validator,
        IUsernameProvider usernameProvider)
        : base(repository, adapter, validator, usernameProvider) { }
}

/// <summary>
/// Service layer base for entities with audit fields.
///
/// Automatically populates CreatedBy/CreatedDate on new items
/// and LastModifiedBy/LastModifiedDate on every save. No change
/// tracking required — we just always update the last-modified fields.
///
/// Also copies audit fields and the concurrency timestamp back to
/// the model after save so the caller has current values.
/// </summary>
/// <typeparam name="TModel">The domain model type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TIdentity">The primary key type.</typeparam>
public abstract class CoreFieldsServiceLayerBase<TModel, TEntity, TIdentity>
    : ServiceLayerBase<TModel, TEntity, TIdentity>
    where TModel : CoreFieldsDomainModelBase<TIdentity>, new()
    where TEntity : CoreFieldsEntityBase<TIdentity>, new()
    where TIdentity : IEquatable<TIdentity>
{
    /// <summary>The provider used to stamp audit fields with the current username.</summary>
    protected IUsernameProvider UsernameProvider { get; }

    /// <summary>
    /// Creates the service layer with its repository, adapter, validator, and username provider.
    /// </summary>
    protected CoreFieldsServiceLayerBase(
        IAsyncReadableRepository<TEntity, TIdentity> repository,
        AdapterBase<TModel, TEntity, TIdentity> adapter,
        IValidatorStrategy<TModel> validator,
        IUsernameProvider usernameProvider)
        : base(repository, adapter, validator)
    {
        UsernameProvider = usernameProvider
            ?? throw new ArgumentNullException(nameof(usernameProvider));
    }

    /// <summary>
    /// Populates audit fields before the adapter copies model → entity.
    ///
    /// New items (see <see cref="ServiceLayerBase{TModel, TEntity, TIdentity}.IsNew"/>):
    /// sets CreatedBy and CreatedDate.
    /// All items: sets LastModifiedBy and LastModifiedDate.
    /// No change tracking needed — just always update.
    /// </summary>
    protected virtual void PopulateAuditFieldsBeforeSave(TModel model)
    {
        var username = UsernameProvider.Username;

        if (IsNew(model.Id))
        {
            model.SetCreatedFields(username);
        }
        else
        {
            model.SetLastModifiedFields(username);
        }
    }

    /// <summary>
    /// Populates audit fields on a collection of child models.
    /// Call this for each child collection before adapting.
    /// </summary>
    protected virtual void PopulateAuditFieldsBeforeSave(
        IEnumerable<CoreFieldsDomainModelBase<TIdentity>> children)
    {
        var username = UsernameProvider.Username;

        foreach (var child in children)
        {
            if (IsNew(child.Id))
            {
                child.SetCreatedFields(username);
            }
            else
            {
                child.SetLastModifiedFields(username);
            }
        }
    }

    /// <summary>
    /// Called before adapt — populates audit fields on the model
    /// so the adapter carries current values to the entity.
    ///
    /// Override OnPopulateAuditFieldsBeforeSave to also handle
    /// child collections.
    /// </summary>
    protected override void OnBeforeAdapt(TModel model, TEntity entity)
    {
        PopulateAuditFieldsBeforeSave(model);
        OnPopulateAuditFieldsBeforeSave(model);
    }

    /// <summary>
    /// Override to populate audit fields on child domain model collections
    /// before the adapter processes them.
    /// </summary>
    protected virtual void OnPopulateAuditFieldsBeforeSave(TModel model) { }

    /// <summary>
    /// Copies Id, audit fields, and concurrency timestamp from the
    /// saved entity back to the model.
    /// </summary>
    protected override void PopulateFieldsFromEntityAfterSave(
        TEntity fromEntity, TModel toModel)
    {
        base.PopulateFieldsFromEntityAfterSave(fromEntity, toModel);

        toModel.CreatedBy = fromEntity.CreatedBy;
        toModel.CreatedDate = fromEntity.CreatedDate;
        toModel.LastModifiedBy = fromEntity.LastModifiedBy;
        toModel.LastModifiedDate = fromEntity.LastModifiedDate;
        toModel.Timestamp = fromEntity.Timestamp;
    }

    /// <summary>
    /// Helper to copy audit fields from saved entities back to
    /// a collection of child models. Call this after save for
    /// each child collection.
    /// </summary>
    protected virtual void PopulateFieldsFromEntityAfterSave(
        IList<CoreFieldsEntityBase<TIdentity>> fromEntities,
        IList<CoreFieldsDomainModelBase<TIdentity>> toModels)
    {
        foreach (var entity in fromEntities)
        {
            var model = toModels.FirstOrDefault(m => m.Id.Equals(entity.Id));
            if (model == null && !IsNew(entity.Id))
            {
                // New items — match by position isn't reliable,
                // but the Id was just assigned. Try to find by
                // matching new (unassigned) items in order.
                model = toModels.FirstOrDefault(m => IsNew(m.Id));
            }

            if (model != null)
            {
                model.Id = entity.Id;
                model.CreatedBy = entity.CreatedBy;
                model.CreatedDate = entity.CreatedDate;
                model.LastModifiedBy = entity.LastModifiedBy;
                model.LastModifiedDate = entity.LastModifiedDate;
                model.Timestamp = entity.Timestamp;
            }
        }
    }
}


