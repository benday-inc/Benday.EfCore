namespace Benday.EfCore.SqlServer.Entities;

/// <summary>
/// Interface for entities that manage dependent/child entity collections.
/// This is the aggregate root pattern — the parent entity controls the
/// lifecycle of its children during save and delete operations.
/// </summary>
public interface IEntityWithDependents
{
    /// <summary>
    /// Returns the dependent entity collections owned by this aggregate root,
    /// or null if the entity has no children.
    /// </summary>
    IList<IDependentEntityCollection>? GetDependentEntities();
}
