using Benday.Common.Testing;
using Benday.EfCore.SqlServer.TestApi;
using Benday.EfCore.SqlServer.TestApi.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;

namespace Benday.EfCore.SqlServer.UnitTests;

public class AdapterBaseTests : TestClassBase
{
    public AdapterBaseTests(ITestOutputHelper output) : base(output) { }

    private PersonAdapter SystemUnderTest => new();

    [Fact]
    public void Adapt_ModelToEntity_CopiesScalarFields()
    {
        // arrange
        var model = new PersonDomainModel
        {
            Id = 5,
            FirstName = "Ada",
            LastName = "Lovelace",
            Status = "active"
        };
        var entity = new Person();

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        entity.FirstName.ShouldEqual("Ada", "FirstName should copy");
        entity.LastName.ShouldEqual("Lovelace", "LastName should copy");
        entity.Status.ShouldEqual("active", "Status should copy");
    }

    [Fact]
    public void Adapt_EntityToModel_CopiesIdAndFields()
    {
        // arrange
        var entity = new Person { Id = 9, FirstName = "Grace", LastName = "Hopper" };
        var model = new PersonDomainModel();

        // act
        SystemUnderTest.Adapt(entity, model);

        // assert
        model.Id.ShouldEqual(9, "Id should copy entity -> model");
        model.FirstName.ShouldEqual("Grace", "FirstName should copy");
        model.LastName.ShouldEqual("Hopper", "LastName should copy");
    }

    [Fact]
    public void Adapt_Collection_AddsNewChild()
    {
        // arrange — model has a brand-new note (Id == 0)
        var model = new PersonDomainModel
        {
            FirstName = "A",
            LastName = "B",
            Notes = { new PersonNoteDomainModel { NoteText = "new note" } }
        };
        var entity = new Person();

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        entity.Notes.Count.ShouldEqual(1, "New child note should be added");
        entity.Notes[0].NoteText.ShouldEqual("new note", "Child note text should copy");
    }

    [Fact]
    public void Adapt_Collection_UpdatesExistingChildMatchedById()
    {
        // arrange — entity already has note id 3; model updates it
        var model = new PersonDomainModel
        {
            FirstName = "A",
            LastName = "B",
            Notes = { new PersonNoteDomainModel { Id = 3, NoteText = "updated" } }
        };
        var entity = new Person
        {
            Notes = { new PersonNote { Id = 3, NoteText = "original" } }
        };

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        entity.Notes.Count.ShouldEqual(1, "No new note should be added for a matched id");
        entity.Notes[0].NoteText.ShouldEqual("updated", "Matched child should be updated in place");
        AssertThat.IsFalse(entity.Notes[0].IsMarkedForDelete, "Matched child should not be marked for delete");
    }

    [Fact]
    public void Adapt_Collection_MarksMissingChildForDelete()
    {
        // arrange — entity has note id 7, but the model no longer includes it
        var model = new PersonDomainModel
        {
            FirstName = "A",
            LastName = "B"
            // no notes
        };
        var entity = new Person
        {
            Notes = { new PersonNote { Id = 7, NoteText = "to be removed" } }
        };

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        AssertThat.IsTrue(entity.Notes[0].IsMarkedForDelete,
            "A child whose id is missing from the model list should be marked for delete");
    }
}
