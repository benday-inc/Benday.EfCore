using Benday.Common.Testing;
using Benday.EfCore.ServiceLayers;
using Benday.EfCore.Testing.Fakes;
using Benday.EfCore.SqlServer.UnitTests.TestHelpers;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// End-to-end proof that the generic service layer works with Guid identity.
/// Mirrors <see cref="PersonServiceTests"/>.
/// </summary>
public class GuidServiceTests : TestClassBase
{
    public GuidServiceTests(ITestOutputHelper output) : base(output) { }

    private const string TestUsername = "tester@example.com";

    private InMemoryGuidRepository Repository { get; } = new();
    private FakeValidatorStrategy<GuidTestDomainModel> Validator { get; } = new();
    private FakeUsernameProvider UsernameProvider { get; } = new(TestUsername);

    private GuidTestService CreateSystemUnderTest() =>
        new(Repository, new GuidTestAdapter(), Validator, UsernameProvider);

    [Fact]
    public async Task SaveAsync_New_AssignsGuidIdAndPopulatesAuditFields()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new GuidTestDomainModel { Name = "Ada" };

        // act
        await sut.SaveAsync(model);

        // assert
        AssertThat.IsFalse(model.Id == Guid.Empty, "Database-assigned Guid id should be copied back to the model");
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
        var model = new GuidTestDomainModel { Name = "Ada" };

        // act & assert
        await Assert.ThrowsAsync<InvalidObjectException>(() => sut.SaveAsync(model));
    }

    [Fact]
    public async Task SaveAsync_ExistingUnknownId_ThrowsUnknownObjectException()
    {
        // arrange — a random Guid id with nothing in the repository
        var sut = CreateSystemUnderTest();
        var model = new GuidTestDomainModel { Id = Guid.NewGuid(), Name = "Ada" };

        // act & assert
        await Assert.ThrowsAsync<UnknownObjectException>(() => sut.SaveAsync(model));
    }

    [Fact]
    public async Task SaveAsync_Update_PreservesCreatedFields()
    {
        // arrange — first save creates the record
        var sut = CreateSystemUnderTest();
        var model = new GuidTestDomainModel { Name = "Ada" };
        await sut.SaveAsync(model);

        var originalCreatedBy = model.CreatedBy;
        var originalCreatedDate = model.CreatedDate;

        // act — update the same model
        model.Name = "Byron";
        await sut.SaveAsync(model);

        // assert
        model.CreatedBy.ShouldEqual(originalCreatedBy, "CreatedBy should be preserved on update");
        model.CreatedDate.ShouldEqual(originalCreatedDate, "CreatedDate should be preserved on update");
        model.LastModifiedBy.ShouldEqual(TestUsername, "LastModifiedBy should still be the current username");
    }

    [Fact]
    public async Task SaveAsync_WithChildren_PersistsChildrenOnEntity()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new GuidTestDomainModel
        {
            Name = "Ada",
            Children = { new GuidChildDomainModel { Value = "a child" } }
        };

        // act
        await sut.SaveAsync(model);

        // assert — the stored entity has the merged child with an assigned Guid id
        var stored = Repository.Items.Single();
        stored.Children.Count.ShouldEqual(1, "Child should be merged onto the entity");
        AssertThat.IsFalse(stored.Children[0].Id == Guid.Empty, "Child should be assigned a Guid id on save");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAdaptedModel()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new GuidTestDomainModel { Name = "Grace" };
        await sut.SaveAsync(model);

        // act
        var fetched = await sut.GetByIdAsync(model.Id);

        // assert
        AssertThat.IsNotNull(fetched, "Saved item should be retrievable");
        fetched.Name.ShouldEqual("Grace", "Adapted model should carry the entity fields");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAdaptedModels()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        await sut.SaveAsync(new GuidTestDomainModel { Name = "One" });
        await sut.SaveAsync(new GuidTestDomainModel { Name = "Two" });

        // act
        var all = await sut.GetAllAsync();

        // assert
        all.Count.ShouldEqual(2, "All saved items should be returned");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        // arrange
        var sut = CreateSystemUnderTest();
        var model = new GuidTestDomainModel { Name = "Temp" };
        await sut.SaveAsync(model);

        // act
        await sut.DeleteAsync(model);

        // assert
        Repository.Items.Count.ShouldEqual(0, "Entity should be deleted");
        AssertThat.IsTrue(Repository.WasDeleteCalled, "Repository delete should be invoked");
    }
}
