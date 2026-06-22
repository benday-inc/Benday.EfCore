using Benday.EfCore.Testing.Fakes;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>
/// In-memory repository for the Guid-keyed test aggregate. Inherits the
/// generic id-assignment, child-handling, and call tracking behavior from
/// <see cref="InMemoryRepository{T, TIdentity}"/>.
/// </summary>
public class InMemoryGuidRepository : InMemoryRepository<GuidTestEntity, Guid>
{
}
