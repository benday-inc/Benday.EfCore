using Benday.Common.Interfaces;
using Benday.EfCore.SqlServer.Adapters;
using Benday.EfCore.SqlServer.DomainModels;
using Benday.EfCore.SqlServer.Entities;

namespace Benday.EfCore.SqlServer.ServiceLayers;

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
public abstract class CoreFieldsServiceLayerBase<TModel, TEntity>
    : ServiceLayerBase<TModel, TEntity>
    where TModel : CoreFieldsDomainModelBase, new()
    where TEntity : CoreFieldsEntityBase, new()
{
    /// <summary>The provider used to stamp audit fields with the current username.</summary>
    protected IUsernameProvider UsernameProvider { get; }

    /// <summary>
    /// Creates the service layer with its repository, adapter, validator, and username provider.
    /// </summary>
    protected CoreFieldsServiceLayerBase(
        IAsyncReadableRepository<TEntity, int> repository,
        AdapterBase<TModel, TEntity> adapter,
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
    /// New items (Id == 0): sets CreatedBy and CreatedDate.
    /// All items: sets LastModifiedBy and LastModifiedDate.
    /// No change tracking needed — just always update.
    /// </summary>
    protected virtual void PopulateAuditFieldsBeforeSave(TModel model)
    {
        var now = DateTime.UtcNow;
        var username = UsernameProvider.Username;

        if (model.Id == 0)
        {
            model.CreatedBy = username;
            model.CreatedDate = now;
        }

        model.LastModifiedBy = username;
        model.LastModifiedDate = now;
    }

    /// <summary>
    /// Populates audit fields on a collection of child models.
    /// Call this for each child collection before adapting.
    /// </summary>
    protected virtual void PopulateAuditFieldsBeforeSave(
        IEnumerable<CoreFieldsDomainModelBase> children)
    {
        var now = DateTime.UtcNow;
        var username = UsernameProvider.Username;

        foreach (var child in children)
        {
            if (child.Id == 0)
            {
                child.CreatedBy = username;
                child.CreatedDate = now;
            }

            child.LastModifiedBy = username;
            child.LastModifiedDate = now;
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
        IList<CoreFieldsEntityBase> fromEntities,
        IList<CoreFieldsDomainModelBase> toModels)
    {
        foreach (var entity in fromEntities)
        {
            var model = toModels.FirstOrDefault(m => m.Id == entity.Id);
            if (model == null && entity.Id != 0)
            {
                // New items — match by position isn't reliable,
                // but the Id was just assigned. Try to find by
                // matching zero-Id items in order.
                model = toModels.FirstOrDefault(m => m.Id == 0);
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
