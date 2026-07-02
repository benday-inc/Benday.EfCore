using Benday.EfCore.DomainModels;
using Benday.EfCore.Entities;

namespace Benday.EfCore.Adapters;

/// <summary>
/// Non-generic int convenience shim over
/// <see cref="CoreFieldsAdapterBase{TModel, TEntity, TIdentity}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
/// <typeparam name="TModel">The CoreFields domain model type.</typeparam>
/// <typeparam name="TEntity">The CoreFields EF Core entity type.</typeparam>
public abstract class CoreFieldsAdapterBase<TModel, TEntity>
    : CoreFieldsAdapterBase<TModel, TEntity, int>
    where TModel : CoreFieldsDomainModelBase<int>, new()
    where TEntity : CoreFieldsEntityBase<int>, new()
{
}

/// <summary>
/// Adapter base for CoreFields models/entities. Copies the framework-managed
/// fields — Status, the four audit fields, and the optimistic-concurrency
/// token (plus identity, entity → model) — automatically in both directions,
/// leaving subclasses to override only <c>PerformAdapt</c> for their own
/// properties. This is the adapter-tier parallel to
/// <see cref="Entities.CoreFieldsEntityBase{TIdentity}"/>,
/// <see cref="DomainModels.CoreFieldsDomainModelBase{TIdentity}"/>, and the
/// CoreFields service-layer base.
///
/// The framework copy runs via the <see cref="AdapterBase{TModel, TEntity, TIdentity}.CopyFrameworkFields(TModel, TEntity)"/>
/// hook, so the override surface is identical to a plain adapter: you still
/// only implement <c>PerformAdapt</c>.
///
/// Escape hatches: override <c>CopyFrameworkFields</c> to take full control,
/// and call the granular <see cref="CopyAuditFields(TModel, TEntity)"/>,
/// <see cref="CopyStatus(TModel, TEntity)"/>, and
/// <see cref="CopyConcurrencyToken(TModel, TEntity)"/> helpers to reuse only
/// the pieces you want. Override everything and you're on your own — nothing
/// forces the helpers on you.
/// </summary>
/// <typeparam name="TModel">The CoreFields domain model type.</typeparam>
/// <typeparam name="TEntity">The CoreFields EF Core entity type.</typeparam>
/// <typeparam name="TIdentity">The primary key type.</typeparam>
public abstract class CoreFieldsAdapterBase<TModel, TEntity, TIdentity>
    : AdapterBase<TModel, TEntity, TIdentity>
    where TModel : CoreFieldsDomainModelBase<TIdentity>, new()
    where TEntity : CoreFieldsEntityBase<TIdentity>, new()
    where TIdentity : IEquatable<TIdentity>
{
    /// <summary>
    /// Copies framework-managed fields model → entity. Override to take full
    /// control; call the protected Copy* helpers to reuse the pieces you want.
    /// Identity is intentionally not copied in this direction — it is managed
    /// by the collection-merge logic and the database.
    /// </summary>
    protected override void CopyFrameworkFields(TModel fromValue, TEntity toValue)
    {
        CopyStatus(fromValue, toValue);
        CopyAuditFields(fromValue, toValue);
        CopyConcurrencyToken(fromValue, toValue);
    }

    /// <summary>
    /// Copies framework-managed fields entity → model, including identity.
    /// Override to take full control; call the protected Copy* helpers to
    /// reuse the pieces you want.
    /// </summary>
    protected override void CopyFrameworkFields(TEntity fromValue, TModel toValue)
    {
        toValue.Id = fromValue.Id;
        CopyStatus(fromValue, toValue);
        CopyAuditFields(fromValue, toValue);
        CopyConcurrencyToken(fromValue, toValue);
    }

    // --- Reusable building blocks ---

    /// <summary>Copies the application-defined Status value, model → entity.</summary>
    protected void CopyStatus(TModel fromValue, TEntity toValue)
        => toValue.Status = fromValue.Status;

    /// <summary>Copies the application-defined Status value, entity → model.</summary>
    protected void CopyStatus(TEntity fromValue, TModel toValue)
        => toValue.Status = fromValue.Status;

    /// <summary>
    /// Copies the four audit fields (CreatedBy/CreatedDate,
    /// LastModifiedBy/LastModifiedDate), model → entity.
    /// </summary>
    protected void CopyAuditFields(TModel fromValue, TEntity toValue)
    {
        toValue.CreatedBy = fromValue.CreatedBy;
        toValue.CreatedDate = fromValue.CreatedDate;
        toValue.LastModifiedBy = fromValue.LastModifiedBy;
        toValue.LastModifiedDate = fromValue.LastModifiedDate;
    }

    /// <summary>
    /// Copies the four audit fields (CreatedBy/CreatedDate,
    /// LastModifiedBy/LastModifiedDate), entity → model.
    /// </summary>
    protected void CopyAuditFields(TEntity fromValue, TModel toValue)
    {
        toValue.CreatedBy = fromValue.CreatedBy;
        toValue.CreatedDate = fromValue.CreatedDate;
        toValue.LastModifiedBy = fromValue.LastModifiedBy;
        toValue.LastModifiedDate = fromValue.LastModifiedDate;
    }

    /// <summary>Copies the optimistic-concurrency token, model → entity.</summary>
    protected void CopyConcurrencyToken(TModel fromValue, TEntity toValue)
        => toValue.Timestamp = fromValue.Timestamp;

    /// <summary>Copies the optimistic-concurrency token, entity → model.</summary>
    protected void CopyConcurrencyToken(TEntity fromValue, TModel toValue)
        => toValue.Timestamp = fromValue.Timestamp;
}
