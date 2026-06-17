namespace Benday.EfCore.ServiceLayers;

/// <summary>
/// Validation strategy interface. Decouples validation logic from
/// both the domain model and the service layer so each validation
/// rule is independently testable and swappable via DI.
/// </summary>
public interface IValidatorStrategy<T>
{
    /// <summary>
    /// Returns true if the supplied value is valid.
    /// </summary>
    bool IsValid(T validateThis);
}

/// <summary>
/// Provides the current username for audit field population.
///
/// In production, this is typically backed by HttpContext.User.
/// In tests, it's a fake that returns a predictable value.
/// This is why it's an interface — so your service layer never
/// touches HttpContext directly.
/// </summary>
public interface IUsernameProvider
{
    /// <summary>
    /// The current username used to stamp audit fields.
    /// </summary>
    string Username { get; }
}
