using Benday.EfCore.ServiceLayers;

namespace Benday.EfCore.Testing.Fakes;

/// <summary>
/// Fake validator for unit testing. You control the outcome.
/// Set IsValidReturnValue to false to test validation failure paths.
/// Defaults to true so the happy path works without setup.
/// </summary>
public class FakeValidatorStrategy<T> : IValidatorStrategy<T>
{
    /// <summary>The value returned from <see cref="IsValid"/>. Defaults to true.</summary>
    public bool IsValidReturnValue { get; set; } = true;

    /// <summary>True once <see cref="IsValid"/> has been called.</summary>
    public bool WasIsValidCalled { get; private set; }

    /// <summary>The argument passed to the most recent <see cref="IsValid"/> call.</summary>
    public T? IsValidArgumentValue { get; private set; }

    /// <inheritdoc />
    public bool IsValid(T validateThis)
    {
        WasIsValidCalled = true;
        IsValidArgumentValue = validateThis;
        return IsValidReturnValue;
    }
}

/// <summary>
/// Fake username provider for unit testing.
/// Returns a predictable username so audit field tests are deterministic.
/// </summary>
public class FakeUsernameProvider : IUsernameProvider
{
    /// <summary>
    /// Creates the fake with a predictable username (default "testuser@test.com").
    /// </summary>
    public FakeUsernameProvider(string username = "testuser@test.com")
    {
        Username = username;
    }

    /// <inheritdoc />
    public string Username { get; set; }
}
