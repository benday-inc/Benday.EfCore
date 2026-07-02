using Benday.Common.Testing;
using Benday.EfCore.SqlServer.UnitTests.TestHelpers;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// Mirrors <see cref="InMemoryRepositoryTests"/> but with Guid identity.
/// </summary>
public class GuidInMemoryRepositoryTests : TestClassBase
{
    public GuidInMemoryRepositoryTests(ITestOutputHelper output) : base(output) { }

    private InMemoryGuidRepository SystemUnderTest { get; } = new();

    [Fact]
    public async Task SaveAsync_AssignsGuidId_WhenNew()
    {
        // arrange
        var entity = new GuidTestEntity { Name = "Ada" };

        // act
        await SystemUnderTest.SaveAsync(entity);

        // assert
        AssertThat.IsFalse(entity.Id == Guid.Empty, "A new entity should be assigned a non-empty Guid id");
        AssertThat.IsTrue(SystemUnderTest.WasSaveCalled, "WasSaveCalled should be tracked");
    }

    [Fact]
    public async Task SaveAsync_AssignsGuidIdsToNewChildren()
    {
        // arrange
        var entity = new GuidTestEntity
        {
            Name = "Ada",
            Children = { new GuidChildEntity { Value = "first" }, new GuidChildEntity { Value = "second" } }
        };

        // act
        await SystemUnderTest.SaveAsync(entity);

        // assert
        AssertThat.IsTrue(entity.Children.All(c => c.Id != Guid.Empty), "New child entities should get Guid ids");
    }

    [Fact]
    public async Task SaveAsync_PrunesChildrenMarkedForDelete()
    {
        // arrange
        var entity = new GuidTestEntity
        {
            Name = "Ada",
            Children =
            {
                new GuidChildEntity { Id = Guid.NewGuid(), Value = "keep" },
                new GuidChildEntity { Id = Guid.NewGuid(), Value = "remove", IsMarkedForDelete = true }
            }
        };

        // act
        await SystemUnderTest.SaveAsync(entity);

        // assert
        entity.Children.Count.ShouldEqual(1, "Child marked for delete should be pruned during save");
        AssertThat.IsFalse(entity.Children.Any(c => c.Value == "remove"), "The removed child should be gone");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatch_ByGuidId()
    {
        // arrange
        var entity = new GuidTestEntity { Name = "Grace" };
        await SystemUnderTest.SaveAsync(entity);

        // act
        var found = await SystemUnderTest.GetByIdAsync(entity.Id);

        // assert
        AssertThat.IsNotNull(found, "Saved entity should be retrievable by Guid id");
        found.Name.ShouldEqual("Grace", "Retrieved entity should match");
        SystemUnderTest.GetByIdArgumentValue.ShouldEqual(entity.Id, "GetByIdArgumentValue should be tracked");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAll()
    {
        // arrange
        await SystemUnderTest.SaveAsync(new GuidTestEntity { Name = "A" });
        await SystemUnderTest.SaveAsync(new GuidTestEntity { Name = "B" });

        // act
        var all = await SystemUnderTest.GetAllAsync();

        // assert
        all.Count.ShouldEqual(2, "GetAll should return both saved entities");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        // arrange
        var entity = new GuidTestEntity { Name = "Temp" };
        await SystemUnderTest.SaveAsync(entity);

        // act
        await SystemUnderTest.DeleteAsync(entity);

        // assert
        SystemUnderTest.Items.Count.ShouldEqual(0, "Entity should be removed");
        AssertThat.IsTrue(SystemUnderTest.WasDeleteCalled, "WasDeleteCalled should be tracked");
    }
}
