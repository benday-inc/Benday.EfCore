using Benday.Common.Testing;
using Benday.EfCore.SqlServer.Adapters;
using Benday.EfCore.SqlServer.TestApi;
using Benday.EfCore.SqlServer.TestApi.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// Adapter that returns a configurable <see cref="AdapterAction"/> from its
/// model-to-entity BeforeAdapt hook, so the action handling can be tested.
/// </summary>
internal class ConfigurableActionPersonAdapter : PersonAdapter
{
    public AdapterAction ActionToReturn { get; set; } = AdapterAction.Adapt;

    protected override AdapterAction BeforeAdapt(PersonDomainModel fromValue, Person toValue)
        => ActionToReturn;
}

public class AdapterActionTests : TestClassBase
{
    public AdapterActionTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void Skip_LeavesTargetUntouched()
    {
        // arrange
        var adapter = new ConfigurableActionPersonAdapter { ActionToReturn = AdapterAction.Skip };
        var model = new PersonDomainModel { FirstName = "Ada", LastName = "Lovelace" };
        var entity = new Person();

        // act
        adapter.Adapt(model, entity);

        // assert
        entity.FirstName.ShouldEqual(string.Empty, "Skip should not copy fields");
        AssertThat.IsFalse(entity.IsMarkedForDelete, "Skip should not mark for delete");
    }

    [Fact]
    public void Delete_MarksTargetForDelete()
    {
        // arrange
        var adapter = new ConfigurableActionPersonAdapter { ActionToReturn = AdapterAction.Delete };
        var model = new PersonDomainModel { FirstName = "Ada", LastName = "Lovelace" };
        var entity = new Person();

        // act
        adapter.Adapt(model, entity);

        // assert
        AssertThat.IsTrue(entity.IsMarkedForDelete, "Delete action should set IsMarkedForDelete");
        entity.FirstName.ShouldEqual(string.Empty, "Delete should not copy fields");
    }
}
