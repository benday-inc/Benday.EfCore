using Benday.EfCore.Entities;
using Benday.EfCore.Testing.Fakes;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>
/// Minimal string-keyed entity for testing the string <c>GenerateId</c> path.
/// </summary>
public class StringTestEntity : EntityBase<string>
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>In-memory repository for the string-keyed test entity.</summary>
public class InMemoryStringRepository : InMemoryRepository<StringTestEntity, string>
{
}
