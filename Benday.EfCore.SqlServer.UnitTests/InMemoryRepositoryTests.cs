using Benday.Common.Testing;
using Benday.EfCore.SqlServer.Testing.Fakes;
using Benday.EfCore.SqlServer.TestApi;

namespace Benday.EfCore.SqlServer.UnitTests;

public class InMemoryRepositoryTests : TestClassBase
{
    public InMemoryRepositoryTests(ITestOutputHelper output) : base(output) { }

    private InMemoryRepository<Person> SystemUnderTest { get; } = new();

    [Fact]
    public async Task SaveAsync_AssignsId_WhenNew()
    {
        // arrange
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        AssertThat.IsTrue(person.Id > 0, "A new entity should be assigned an id");
        AssertThat.IsTrue(SystemUnderTest.WasSaveCalled, "WasSaveCalled should be tracked");
        AssertThat.AreEqual(person, SystemUnderTest.SaveArgumentValue!, "SaveArgumentValue should be tracked");
    }

    [Fact]
    public async Task SaveAsync_AssignsIdsToNewChildren()
    {
        // arrange
        var person = new Person
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Notes = { new PersonNote { NoteText = "first" }, new PersonNote { NoteText = "second" } }
        };

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        AssertThat.IsTrue(person.Notes.All(n => n.Id > 0), "New child notes should get ids");
    }

    [Fact]
    public async Task SaveAsync_PrunesChildrenMarkedForDelete()
    {
        // arrange
        var person = new Person
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Notes =
            {
                new PersonNote { Id = 1, NoteText = "keep" },
                new PersonNote { Id = 2, NoteText = "remove", IsMarkedForDelete = true }
            }
        };

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        person.Notes.Count.ShouldEqual(1, "Child marked for delete should be pruned during save");
        AssertThat.IsFalse(person.Notes.Any(n => n.Id == 2), "Note 2 should be removed");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatch_AndTracks()
    {
        // arrange
        var person = new Person { FirstName = "Grace", LastName = "Hopper" };
        await SystemUnderTest.SaveAsync(person);

        // act
        var found = await SystemUnderTest.GetByIdAsync(person.Id);

        // assert
        AssertThat.IsNotNull(found, "Saved entity should be retrievable by id");
        found.LastName.ShouldEqual("Hopper", "Retrieved entity should match");
        AssertThat.IsTrue(SystemUnderTest.WasGetByIdCalled, "WasGetByIdCalled should be tracked");
        SystemUnderTest.GetByIdArgumentValue.ShouldEqual(person.Id, "GetByIdArgumentValue should be tracked");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAll()
    {
        // arrange
        await SystemUnderTest.SaveAsync(new Person { FirstName = "A", LastName = "One" });
        await SystemUnderTest.SaveAsync(new Person { FirstName = "B", LastName = "Two" });

        // act
        var all = await SystemUnderTest.GetAllAsync();

        // assert
        all.Count.ShouldEqual(2, "GetAll should return both saved entities");
        AssertThat.IsTrue(SystemUnderTest.WasGetAllCalled, "WasGetAllCalled should be tracked");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity_AndTracks()
    {
        // arrange
        var person = new Person { FirstName = "Temp", LastName = "Person" };
        await SystemUnderTest.SaveAsync(person);

        // act
        await SystemUnderTest.DeleteAsync(person);

        // assert
        SystemUnderTest.Items.Count.ShouldEqual(0, "Entity should be removed");
        AssertThat.IsTrue(SystemUnderTest.WasDeleteCalled, "WasDeleteCalled should be tracked");
    }

    [Fact]
    public async Task ResetMethodCallTrackers_ClearsFlags()
    {
        // arrange
        await SystemUnderTest.SaveAsync(new Person { FirstName = "A", LastName = "B" });

        // act
        SystemUnderTest.ResetMethodCallTrackers();

        // assert
        AssertThat.IsFalse(SystemUnderTest.WasSaveCalled, "Trackers should reset");
        AssertThat.IsNull(SystemUnderTest.SaveArgumentValue, "Captured arg should reset");
    }
}
