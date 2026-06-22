using Benday.Common.Testing;
using Benday.EfCore.ServiceLayers;
using Benday.EfCore.SqlServer.TestApi.DomainModels;

namespace Benday.EfCore.SqlServer.UnitTests;

public class DefaultValidatorStrategyTests : TestClassBase
{
    public DefaultValidatorStrategyTests(ITestOutputHelper output) : base(output) { }

    private DefaultValidatorStrategy<PersonDomainModel> SystemUnderTest { get; } = new();

    [Fact]
    public void IsValid_ReturnsTrue_WhenRequiredFieldsPresent()
    {
        // arrange
        var model = new PersonDomainModel { FirstName = "Ada", LastName = "Lovelace" };

        // act
        var result = SystemUnderTest.IsValid(model);

        // assert
        AssertThat.IsTrue(result, "Model with all required fields should be valid");
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenRequiredFieldMissing()
    {
        // arrange — FirstName left empty; [Required] on a string rejects empty
        var model = new PersonDomainModel { FirstName = "", LastName = "" };

        // act
        var result = SystemUnderTest.IsValid(model);

        // assert
        AssertThat.IsFalse(result, "Model missing required fields should be invalid");
        AssertThat.IsNotNull(SystemUnderTest.LastValidationResult, "Validation results should be captured");
        AssertThat.IsTrue(SystemUnderTest.LastValidationResult!.Count > 0, "There should be validation errors");
    }
}
