using System.ComponentModel.DataAnnotations.Schema;
using Benday.Common.Interfaces;

namespace Benday.EfCore.Entities;

/// <summary>
/// Non-generic int convenience shim over <see cref="EntityBase{TIdentity}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
public abstract class EntityBase : EntityBase<int>
{
}

/// <summary>
/// Base class for EF Core entities. Implements the shared
/// IEntityIdentity and IDeleteable interfaces from Benday.Common.Interfaces.
///
/// The identity type is generic (<typeparamref name="TIdentity"/>) so the same
/// base class supports int, Guid, string, or any other key type. Int consumers
/// use the non-generic <see cref="EntityBase"/> shim and never type <c>&lt;int&gt;</c>.
///
/// IsMarkedForDelete is [NotMapped] — it only exists in memory to signal
/// the DependentEntityCollection to remove this item during save.
/// </summary>
/// <typeparam name="TIdentity">The primary key type.</typeparam>
public abstract class EntityBase<TIdentity>
    : IEntityIdentity<TIdentity>, IDeleteable, IEntityWithDependents
    where TIdentity : IEquatable<TIdentity>
{
    /// <summary>
    /// Primary key. The default value (0 for int, Guid.Empty for Guid, null for
    /// string) means the entity has not yet been persisted.
    /// </summary>
    public TIdentity Id { get; set; } = default!;

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


