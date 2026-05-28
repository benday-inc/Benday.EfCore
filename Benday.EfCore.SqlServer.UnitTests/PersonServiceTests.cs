using Benday.Common.Testing;
using Benday.EfCore.SqlServer.ServiceLayers;
using Benday.EfCore.SqlServer.Testing.Fakes;
using Benday.EfCore.SqlServer.TestApi.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;
using Benday.EfCore.SqlServer.TestApi.Services;
using Benday.EfCore.SqlServer.UnitTests.TestHelpers;

namespace Benday.EfCore.SqlServer.UnitTests;

public class PersonServiceTests : TestClassBase
{
    public PersonServiceTests(ITestOutputHelper output) : base(output) { }

    private const string TestUsername = "tester@example.com";

    private InMemoryPersonRepository Repository { get; } = new();
    private FakeValidatorStrategy<PersonDomainModel> Validator { get; } = new();
    private FakeUsernameProvider UsernameProvider { get; } = new(TestUsername);

    private PersonService CreateSystemUnderTest() =>
        new(Repository, new PersonAdapter(), Validator, UsernameProvider);

    [Fact]
    public async Task SaveAsync_New_AssignsIdAndPopulatesAuditFields()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new PersonDomainModel { FirstName = "Ada", LastName = "Lovelace" };

        // act
        await sut.SaveAsync(model);

        // assert
        AssertThat.IsTrue(model.Id > 0, "Database-assigned id should be copied back to the model");
        AssertThat.IsTrue(Validator.WasIsValidCalled, "Validator should be invoked");
        model.CreatedBy.ShouldEqual(TestUsername, "CreatedBy should be the current username on insert");
        model.LastModifiedBy.ShouldEqual(TestUsername, "LastModifiedBy should be the current username");
        AssertThat.IsTrue(model.CreatedDate != default, "CreatedDate should be set on insert");
    }

    [Fact]
    public async Task SaveAsync_Invalid_ThrowsInvalidObjectException()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        Validator.IsValidReturnValue = false;
        var model = new PersonDomainModel { FirstName = "Ada", LastName = "Lovelace" };

        // act & assert
        await Assert.ThrowsAsync<InvalidObjectException>(() => sut.SaveAsync(model));
    }

    [Fact]
    public async Task SaveAsync_ExistingUnknownId_ThrowsUnknownObjectException()
    {
        // arrange — id set but nothing in the repository
        var sut = CreateSystemUnderTest();
        var model = new PersonDomainModel { Id = 999, FirstName = "Ada", LastName = "Lovelace" };

        // act & assert
        await Assert.ThrowsAsync<UnknownObjectException>(() => sut.SaveAsync(model));
    }

    [Fact]
    public async Task SaveAsync_Update_PreservesCreatedFields()
    {
        // arrange — first save creates the record
        var sut = CreateSystemUnderTest();
        var model = new PersonDomainModel { FirstName = "Ada", LastName = "Lovelace" };
        await sut.SaveAsync(model);

        var originalCreatedBy = model.CreatedBy;
        var originalCreatedDate = model.CreatedDate;

        // act — update the same model
        model.LastName = "Byron";
        await sut.SaveAsync(model);

        // assert
        model.CreatedBy.ShouldEqual(originalCreatedBy, "CreatedBy should be preserved on update");
        model.CreatedDate.ShouldEqual(originalCreatedDate, "CreatedDate should be preserved on update");
        model.LastModifiedBy.ShouldEqual(TestUsername, "LastModifiedBy should still be the current username");
    }

    [Fact]
    public async Task SaveAsync_WithChildNotes_PersistsNotesOnEntity()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new PersonDomainModel
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Notes = { new PersonNoteDomainModel { NoteText = "a note" } }
        };

        // act
        await sut.SaveAsync(model);

        // assert — the stored entity has the merged child note with an assigned id
        var stored = Repository.Items.Single();
        stored.Notes.Count.ShouldEqual(1, "Child note should be merged onto the entity");
        AssertThat.IsTrue(stored.Notes[0].Id > 0, "Child note should be assigned an id on save");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAdaptedModel()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new PersonDomainModel { FirstName = "Grace", LastName = "Hopper" };
        await sut.SaveAsync(model);

        // act
        var fetched = await sut.GetByIdAsync(model.Id);

        // assert
        AssertThat.IsNotNull(fetched, "Saved person should be retrievable");
        fetched.FirstName.ShouldEqual("Grace", "Adapted model should carry the entity fields");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAdaptedModels()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        await sut.SaveAsync(new PersonDomainModel { FirstName = "A", LastName = "One" });
        await sut.SaveAsync(new PersonDomainModel { FirstName = "B", LastName = "Two" });

        // act
        var all = await sut.GetAllAsync();

        // assert
        all.Count.ShouldEqual(2, "All saved people should be returned");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new PersonDomainModel { FirstName = "Temp", LastName = "Person" };
        await sut.SaveAsync(model);

        // act
        await sut.DeleteAsync(model);

        // assert
        Repository.Items.Count.ShouldEqual(0, "Entity should be deleted");
        AssertThat.IsTrue(Repository.WasDeleteCalled, "Repository delete should be invoked");
    }
}
