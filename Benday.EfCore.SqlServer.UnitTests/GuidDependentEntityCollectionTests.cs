using Benday.Common.Interfaces;
using Benday.Common.Testing;
using Benday.EfCore.Entities;
using Benday.EfCore.SqlServer.UnitTests.TestHelpers;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// Mirrors <see cref="DependentEntityCollectionTests"/> with Guid-keyed children.
/// </summary>
public class GuidDependentEntityCollectionTests : TestClassBase
{
    public GuidDependentEntityCollectionTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void AfterSave_PrunesItemsMarkedForDelete()
    {
        // arrange
        var removeId = Guid.NewGuid();
        var children = new List<GuidChildEntity>
        {
            new() { Id = Guid.NewGuid(), Value = "keep" },
            new() { Id = removeId, Value = "remove", IsMarkedForDelete = true },
            new() { Id = Guid.NewGuid(), Value = "keep too" }
        };
        var sut = new DependentEntityCollection<GuidChildEntity, Guid>(children);

        // act
        sut.AfterSave();

        // assert
        children.Count.ShouldEqual(2, "Marked-for-delete item should be pruned");
        AssertThat.IsFalse(children.Any(c => c.Id == removeId), "The removed child should be gone");
    }

    [Fact]
    public void GetItems_ReturnsAllItems_AsGuidIdentities()
    {
        // arrange
        var children = new List<GuidChildEntity>
        {
            new() { Id = Guid.NewGuid(), Value = "a" },
            new() { Id = Guid.NewGuid(), Value = "b" }
        };
        var sut = new DependentEntityCollection<GuidChildEntity, Guid>(children);

        // act
        var items = sut.GetItems().ToList();

        // assert
        items.Count.ShouldEqual(2, "GetItems should return both items");
        AssertThat.IsTrue(items.All(i => i is IEntityIdentity<Guid>),
            "Items should be exposed as IEntityIdentity<Guid>");
    }
}
