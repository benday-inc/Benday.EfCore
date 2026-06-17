using Benday.Common.Testing;
using Benday.EfCore.Entities;
using Benday.EfCore.SqlServer.TestApi;

namespace Benday.EfCore.SqlServer.UnitTests;

public class DependentEntityCollectionTests : TestClassBase
{
    public DependentEntityCollectionTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void AfterSave_PrunesItemsMarkedForDelete()
    {
        // arrange
        var notes = new List<PersonNote>
        {
            new() { Id = 1, NoteText = "keep" },
            new() { Id = 2, NoteText = "remove", IsMarkedForDelete = true },
            new() { Id = 3, NoteText = "keep too" }
        };
        var sut = new DependentEntityCollection<PersonNote>(notes);

        // act
        sut.AfterSave();

        // assert
        notes.Count.ShouldEqual(2, "Marked-for-delete item should be pruned");
        AssertThat.IsFalse(notes.Any(n => n.Id == 2), "Item 2 should be gone");
    }

    [Fact]
    public void GetItems_ReturnsAllItems()
    {
        // arrange
        var notes = new List<PersonNote>
        {
            new() { Id = 1, NoteText = "a" },
            new() { Id = 2, NoteText = "b" }
        };
        var sut = new DependentEntityCollection<PersonNote>(notes);

        // act
        var items = sut.GetItems().ToList();

        // assert
        items.Count.ShouldEqual(2, "GetItems should return both items");
    }
}
