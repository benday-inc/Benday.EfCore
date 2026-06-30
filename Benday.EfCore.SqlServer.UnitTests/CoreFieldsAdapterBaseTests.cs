using Benday.Common.Testing;
using Benday.EfCore.SqlServer.TestApi;
using Benday.EfCore.SqlServer.TestApi.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// Adapter that overrides the <c>CopyFrameworkFields</c> hook to reuse only
/// the audit-field helper — proving the seam is overridable and that an
/// override replaces (not augments) the default framework copy.
/// </summary>
internal class AuditOnlyPersonAdapter : PersonAdapter
{
    protected override void CopyFrameworkFields(PersonDomainModel fromValue, Person toValue)
        => CopyAuditFields(fromValue, toValue);
}

/// <summary>
/// Verifies that <c>CoreFieldsAdapterBase</c> copies the framework-managed
/// fields (Status, audit fields, concurrency token, and identity entity → model)
/// automatically, without the concrete adapter having to copy them.
/// </summary>
public class CoreFieldsAdapterBaseTests : TestClassBase
{
    public CoreFieldsAdapterBaseTests(ITestOutputHelper output) : base(output) { }

    private PersonAdapter SystemUnderTest => new();

    [Fact]
    public void Adapt_ModelToEntity_CopiesFrameworkFields()
    {
        // arrange
        var created = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2021, 2, 2, 0, 0, 0, DateTimeKind.Utc);
        var token = new byte[] { 1, 2, 3, 4 };

        var model = new PersonDomainModel
        {
            Id = 5,
            FirstName = "Ada",
            LastName = "Lovelace",
            Status = "active",
            CreatedBy = "creator",
            CreatedDate = created,
            LastModifiedBy = "modifier",
            LastModifiedDate = modified,
            Timestamp = token
        };
        var entity = new Person();

        // act
        SystemUnderTest.Adapt(model, entity);

        // assert — none of these are copied by PersonAdapter.PerformAdapt
        entity.Status.ShouldEqual("active", "Status should be copied by the base");
        entity.CreatedBy.ShouldEqual("creator", "CreatedBy should be copied by the base");
        entity.CreatedDate.ShouldEqual(created, "CreatedDate should be copied by the base");
        entity.LastModifiedBy.ShouldEqual("modifier", "LastModifiedBy should be copied by the base");
        entity.LastModifiedDate.ShouldEqual(modified, "LastModifiedDate should be copied by the base");
        AssertThat.IsNotNull(entity.Timestamp, "Timestamp should be copied by the base");
        entity.Timestamp!.Length.ShouldEqual(4, "Timestamp bytes should round-trip");
    }

    [Fact]
    public void Adapt_EntityToModel_CopiesFrameworkFieldsAndIdentity()
    {
        // arrange
        var created = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2021, 2, 2, 0, 0, 0, DateTimeKind.Utc);
        var token = new byte[] { 9, 8, 7 };

        var entity = new Person
        {
            Id = 42,
            FirstName = "Grace",
            LastName = "Hopper",
            Status = "retired",
            CreatedBy = "creator",
            CreatedDate = created,
            LastModifiedBy = "modifier",
            LastModifiedDate = modified,
            Timestamp = token
        };
        var model = new PersonDomainModel();

        // act
        SystemUnderTest.Adapt(entity, model);

        // assert
        model.Id.ShouldEqual(42, "Id should be copied entity -> model by the base");
        model.Status.ShouldEqual("retired", "Status should be copied by the base");
        model.CreatedBy.ShouldEqual("creator", "CreatedBy should be copied by the base");
        model.CreatedDate.ShouldEqual(created, "CreatedDate should be copied by the base");
        model.LastModifiedBy.ShouldEqual("modifier", "LastModifiedBy should be copied by the base");
        model.LastModifiedDate.ShouldEqual(modified, "LastModifiedDate should be copied by the base");
        AssertThat.IsNotNull(model.Timestamp, "Timestamp should be copied by the base");
        model.Timestamp!.Length.ShouldEqual(3, "Timestamp bytes should round-trip");
    }

    [Fact]
    public void CopyFrameworkFields_OverrideReplacesDefaultCopy()
    {
        // arrange — an adapter that overrides the hook to reuse ONLY the audit
        // helper. The default would also copy Status + the concurrency token.
        var adapter = new AuditOnlyPersonAdapter();
        var model = new PersonDomainModel
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Status = "active",
            CreatedBy = "creator",
            Timestamp = new byte[] { 1, 2, 3, 4 }
        };
        var entity = new Person();

        // act
        adapter.Adapt(model, entity);

        // assert — the override is honored: audit field reused...
        entity.CreatedBy.ShouldEqual("creator", "CopyAuditFields should still run via the override");
        // ...but Status and Timestamp are NOT copied, proving the override
        // replaces the default framework copy rather than adding to it.
        entity.Status.ShouldEqual(string.Empty, "Status should not be copied when the override omits CopyStatus");
        AssertThat.IsNull(entity.Timestamp, "Timestamp should not be copied when the override omits CopyConcurrencyToken");
    }
}
