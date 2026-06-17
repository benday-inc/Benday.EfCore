using Benday.Common.Testing;
using Benday.EfCore.SqlServer.UnitTests.TestHelpers;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// Mirrors <see cref="AdapterBaseTests"/> but with Guid identity, proving the
/// adapter's collection-merge logic works for non-int keys.
/// </summary>
public class GuidAdapterTests : TestClassBase
{
    public GuidAdapterTests(ITestOutputHelper output) : base(output) { }

    private GuidTestAdapter SystemUnderTest => new();

    [Fact]
    public void Adapt_ModelToEntity_CopiesScalarFields()
    {
        // arrange
        var model = new GuidTestDomainModel
        {
            Id = Guid.NewGuid(),
            Name = "Ada",
            Status = "active"
        };
        var entity = new GuidTestEntity();

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        entity.Name.ShouldEqual("Ada", "Name should copy");
        entity.Status.ShouldEqual("active", "Status should copy");
    }

    [Fact]
    public void Adapt_EntityToModel_CopiesIdAndFields()
    {
        // arrange
        var id = Guid.NewGuid();
        var entity = new GuidTestEntity { Id = id, Name = "Grace" };
        var model = new GuidTestDomainModel();

        // act
        SystemUnderTest.Adapt(entity, model);

        // assert
        model.Id.ShouldEqual(id, "Guid id should copy entity -> model");
        model.Name.ShouldEqual("Grace", "Name should copy");
    }

    [Fact]
    public void Adapt_Collection_AddsNewChild()
    {
        // arrange — child with Id == Guid.Empty is treated as new
        var model = new GuidTestDomainModel
        {
            Name = "A",
            Children = { new GuidChildDomainModel { Value = "new child" } }
        };
        var entity = new GuidTestEntity();

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        entity.Children.Count.ShouldEqual(1, "New child should be added");
        entity.Children[0].Value.ShouldEqual("new child", "Child value should copy");
    }

    [Fact]
    public void Adapt_Collection_UpdatesExistingChildMatchedById()
    {
        // arrange — entity already has the child; model updates it by matching Guid id
        var childId = Guid.NewGuid();
        var model = new GuidTestDomainModel
        {
            Name = "A",
            Children = { new GuidChildDomainModel { Id = childId, Value = "updated" } }
        };
        var entity = new GuidTestEntity
        {
            Children = { new GuidChildEntity { Id = childId, Value = "original" } }
        };

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        entity.Children.Count.ShouldEqual(1, "No new child should be added for a matched id");
        entity.Children[0].Value.ShouldEqual("updated", "Matched child should be updated in place");
        AssertThat.IsFalse(entity.Children[0].IsMarkedForDelete, "Matched child should not be marked for delete");
    }

    [Fact]
    public void Adapt_Collection_MarksMissingChildForDelete()
    {
        // arrange — entity has a child whose Guid id is missing from the model list
        var model = new GuidTestDomainModel
        {
            Name = "A"
            // no children
        };
        var entity = new GuidTestEntity
        {
            Children = { new GuidChildEntity { Id = Guid.NewGuid(), Value = "to be removed" } }
        };

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert
        AssertThat.IsTrue(entity.Children[0].IsMarkedForDelete,
            "A child whose Guid id is missing from the model list should be marked for delete");
    }
}
