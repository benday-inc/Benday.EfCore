using Benday.Common.Testing;
using Benday.EfCore.Testing.Fakes;
using Benday.EfCore.SqlServer.TestApi;
using Benday.EfCore.SqlServer.UnitTests.TestHelpers;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// Exercises the <see cref="InMemoryRepository{T, TIdentity}"/> GenerateId seam
/// by saving entities with default ids and inspecting what gets assigned.
/// </summary>
public class GenerateIdTests : TestClassBase
{
    public GenerateIdTests(ITestOutputHelper output) : base(output) { }

    private static Guid Seq(int n) =>
        Guid.Parse($"00000000-0000-0000-0000-{n:D12}");

    [Fact]
    public async Task GenerateId_Int_ProducesSequentialInts()
    {
        // arrange
        var repo = new InMemoryRepository<Person>();
        var a = new Person { FirstName = "A", LastName = "One" };
        var b = new Person { FirstName = "B", LastName = "Two" };
        var c = new Person { FirstName = "C", LastName = "Three" };

        // act
        await repo.SaveAsync(a);
        await repo.SaveAsync(b);
        await repo.SaveAsync(c);

        // assert
        a.Id.ShouldEqual(1, "First int id should be 1");
        b.Id.ShouldEqual(2, "Second int id should be 2");
        c.Id.ShouldEqual(3, "Third int id should be 3");
    }

    [Fact]
    public async Task GenerateId_Guid_ProducesSequentialDeterministicGuids()
    {
        // arrange
        var repo = new InMemoryGuidRepository();
        var a = new GuidTestEntity { Name = "A" };
        var b = new GuidTestEntity { Name = "B" };
        var c = new GuidTestEntity { Name = "C" };

        // act
        await repo.SaveAsync(a);
        await repo.SaveAsync(b);
        await repo.SaveAsync(c);

        // assert
        a.Id.ShouldEqual(Seq(1), "First guid id should be ...0001");
        b.Id.ShouldEqual(Seq(2), "Second guid id should be ...0002");
        c.Id.ShouldEqual(Seq(3), "Third guid id should be ...0003");
    }

    [Fact]
    public async Task GenerateId_Guid_ChildrenGetSequentialIdsAfterParent()
    {
        // arrange
        var repo = new InMemoryGuidRepository();
        var parent = new GuidTestEntity
        {
            Name = "parent",
            Children =
            {
                new GuidChildEntity { Value = "first" },
                new GuidChildEntity { Value = "second" }
            }
        };

        // act
        await repo.SaveAsync(parent);

        // assert — parent gets ...0001, children get ...0002 and ...0003, no collisions
        parent.Id.ShouldEqual(Seq(1), "Parent should get the first id");
        parent.Children[0].Id.ShouldEqual(Seq(2), "First child should get the second id");
        parent.Children[1].Id.ShouldEqual(Seq(3), "Second child should get the third id");

        var allIds = new[] { parent.Id, parent.Children[0].Id, parent.Children[1].Id };
        allIds.Distinct().Count().ShouldEqual(3, "All assigned ids should be unique");
    }

    [Fact]
    public async Task GenerateId_String_ProducesSequentialStringIds()
    {
        // arrange
        var repo = new InMemoryStringRepository();
        var a = new StringTestEntity { Name = "A" };
        var b = new StringTestEntity { Name = "B" };
        var c = new StringTestEntity { Name = "C" };

        // act
        await repo.SaveAsync(a);
        await repo.SaveAsync(b);
        await repo.SaveAsync(c);

        // assert
        a.Id.ShouldEqual("1", "First string id should be \"1\"");
        b.Id.ShouldEqual("2", "Second string id should be \"2\"");
        c.Id.ShouldEqual("3", "Third string id should be \"3\"");
    }
}
