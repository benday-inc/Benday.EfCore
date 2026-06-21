using Benday.Common.Interfaces;

namespace Benday.EfCore.DomainModels;

/// <summary>
/// Base class for domain models. Uses IEntityIdentity{int} from
/// the shared interface package so domain models and entities
/// share the same identity contract.
///
/// Domain models are NOT EF Core entities. They live on the business
/// logic side of the adapter boundary. EF Core should never see these.
/// </summary>
/// <typeparam name="TIdentity">The identity type.</typeparam>
public abstract class DomainModelBase<TIdentity> : IEntityIdentity<TIdentity>
    where TIdentity : IEquatable<TIdentity>
{
    /// <summary>
    /// Identity of the domain model. The default value (0 for int, Guid.Empty
    /// for Guid, null for string) means not yet persisted.
    /// </summary>
    public TIdentity Id { get; set; } = default!;
}

/// <summary>
/// Non-generic int convenience shim over <see cref="DomainModelBase{TIdentity}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
public abstract class DomainModelBase : DomainModelBase<int>
{
}

/// <summary>
/// Domain model base class with audit fields and optimistic concurrency.
///
/// The Timestamp property carries the concurrency token across the
/// adapter boundary so the service layer can detect conflicting updates
/// without touching the entity layer directly.
/// </summary>
/// <typeparam name="TIdentity">The identity type.</typeparam>
public abstract class CoreFieldsDomainModelBase<TIdentity> : DomainModelBase<TIdentity>
    where TIdentity : IEquatable<TIdentity>
{
    /// <summary>
    /// Application-defined status value for the model.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Username that created the item.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of when the item was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Username that last modified the item.
    /// </summary>
    public string LastModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the last modification.
    /// </summary>
    public DateTime LastModifiedDate { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Carried across the adapter
    /// boundary so the entity layer can detect conflicting updates.
    /// </summary>
    public byte[]? Timestamp { get; set; }

    /// <summary>
    /// Sets the created and last modified audit fields for a new model.
    /// Call this on insert. Sets CreatedBy, CreatedDate, LastModifiedBy,
    /// and LastModifiedDate to the supplied username and <see cref="DateTime.UtcNow"/>.
    /// </summary>
    /// <param name="byUsername">The username to record as creator and modifier.</param>
    public virtual void SetCreatedFields(string byUsername)
    {
        var now = DateTime.UtcNow;

        CreatedBy = byUsername;
        CreatedDate = now;
        LastModifiedBy = byUsername;
        LastModifiedDate = now;
    }

    /// <summary>
    /// Sets the last modified audit fields for an existing model.
    /// Call this on update. Sets LastModifiedBy and LastModifiedDate
    /// to the supplied username and <see cref="DateTime.UtcNow"/>.
    /// </summary>
    /// <param name="byUsername">The username to record as modifier.</param>
    public virtual void SetLastModifiedFields(string byUsername)
    {
        LastModifiedBy = byUsername;
        LastModifiedDate = DateTime.UtcNow;
    }
}

/// <summary>
/// Non-generic int convenience shim over <see cref="CoreFieldsDomainModelBase{TIdentity}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
public abstract class CoreFieldsDomainModelBase : CoreFieldsDomainModelBase<int>
{
}
