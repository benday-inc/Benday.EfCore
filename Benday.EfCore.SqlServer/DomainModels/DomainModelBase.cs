using Benday.Common.Interfaces;

namespace Benday.EfCore.SqlServer.DomainModels;

/// <summary>
/// Base class for domain models. Uses IEntityIdentity{int} from
/// the shared interface package so domain models and entities
/// share the same identity contract.
///
/// Domain models are NOT EF Core entities. They live on the business
/// logic side of the adapter boundary. EF Core should never see these.
/// </summary>
public abstract class DomainModelBase : IEntityIdentity<int>
{
    /// <summary>
    /// Identity of the domain model. Zero means not yet persisted.
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// Domain model base class with audit fields and optimistic concurrency.
///
/// The Timestamp property carries the concurrency token across the
/// adapter boundary so the service layer can detect conflicting updates
/// without touching the entity layer directly.
/// </summary>
public abstract class CoreFieldsDomainModelBase : DomainModelBase
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
}
