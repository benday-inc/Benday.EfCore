using System.ComponentModel.DataAnnotations.Schema;
using Benday.Common.Interfaces;

namespace Benday.EfCore.SqlServer.Entities;

/// <summary>
/// Base class for EF Core entities. Implements the shared
/// IEntityIdentity and IDeleteable interfaces from Benday.Common.Interfaces.
///
/// IsMarkedForDelete is [NotMapped] — it only exists in memory to signal
/// the DependentEntityCollection to remove this item during save.
/// </summary>
public abstract class EntityBase : IEntityIdentity<int>, IDeleteable, IEntityWithDependents
{
    /// <summary>
    /// Primary key. Zero means the entity has not yet been persisted.
    /// </summary>
    [Column(Order = 0)]
    public int Id { get; set; }

    /// <summary>
    /// In-memory flag signaling that this entity should be removed during
    /// the next save. Not mapped to a database column.
    /// </summary>
    [NotMapped]
    public bool IsMarkedForDelete { get; set; }

    /// <summary>
    /// Override to return dependent entity collections for aggregate root behavior.
    /// Return null if this entity has no children.
    /// </summary>
    public virtual IList<IDependentEntityCollection>? GetDependentEntities() => null;
}
