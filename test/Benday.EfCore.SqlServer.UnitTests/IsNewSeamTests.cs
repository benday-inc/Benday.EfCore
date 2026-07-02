using Benday.Common.Testing;
using Benday.EfCore.SqlServer.UnitTests.TestHelpers;

namespace Benday.EfCore.SqlServer.UnitTests;

/// <summary>
/// Exercises the default <c>IsNew</c> seam (Id == default(TIdentity)) for
/// int, Guid, and string identity types.
/// </summary>
public class IsNewSeamTests : TestClassBase
{
    public IsNewSeamTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void IsNew_Int_Zero_ReturnsTrue()
    {
        var sut = new IsNewProbeAdapter<int>();
        AssertThat.IsTrue(sut.CallIsNew(0), "0 should be treated as new for int");
    }

    [Fact]
    public void IsNew_Int_NonZero_ReturnsFalse()
    {
        var sut = new IsNewProbeAdapter<int>();
        AssertThat.IsFalse(sut.CallIsNew(42), "A non-zero int should not be new");
    }

    [Fact]
    public void IsNew_Guid_Empty_ReturnsTrue()
    {
        var sut = new IsNewProbeAdapter<Guid>();
        AssertThat.IsTrue(sut.CallIsNew(Guid.Empty), "Guid.Empty should be treated as new");
    }

    [Fact]
    public void IsNew_Guid_NonEmpty_ReturnsFalse()
    {
        var sut = new IsNewProbeAdapter<Guid>();
        AssertThat.IsFalse(sut.CallIsNew(Guid.NewGuid()), "A non-empty Guid should not be new");
    }

    [Fact]
    public void IsNew_String_Null_ReturnsTrue()
    {
        var sut = new IsNewProbeAdapter<string>();
        AssertThat.IsTrue(sut.CallIsNew(null!), "A null string should be treated as new");
    }

    [Fact]
    public void IsNew_String_NonNull_ReturnsFalse()
    {
        var sut = new IsNewProbeAdapter<string>();
        AssertThat.IsFalse(sut.CallIsNew("abc"), "A non-null string should not be new");
    }

    [Fact]
    public void IsNew_String_Empty_ReturnsFalse()
    {
        var sut = new IsNewProbeAdapter<string>();
        // Empty string is NOT default(string) (which is null), so it is NOT "new".
        AssertThat.IsFalse(sut.CallIsNew(string.Empty),
            "An empty string is not default(string) and should not be treated as new");
    }
}
