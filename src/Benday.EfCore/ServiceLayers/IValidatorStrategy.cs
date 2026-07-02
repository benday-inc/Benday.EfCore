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
