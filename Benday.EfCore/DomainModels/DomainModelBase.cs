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


