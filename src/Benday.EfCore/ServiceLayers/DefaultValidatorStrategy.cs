using System.ComponentModel.DataAnnotations;

namespace Benday.EfCore.ServiceLayers;

/// <summary>
/// Default validator that uses DataAnnotations attributes on the
/// domain model. Returns validation results for callers that need
/// error details.
/// </summary>
public class DefaultValidatorStrategy<T> : IValidatorStrategy<T>
{
    /// <summary>
    /// The validation results from the most recent <see cref="IsValid"/> call.
    /// </summary>
    public IList<ValidationResult>? LastValidationResult { get; private set; }

    /// <inheritdoc />
    public bool IsValid(T validateThis)
    {
        if (validateThis == null)
            throw new ArgumentNullException(nameof(validateThis));

        var results = new List<ValidationResult>();
        var context = new ValidationContext(validateThis);

        Validator.TryValidateObject(validateThis, context, results, validateAllProperties: true);

        LastValidationResult = results;

        return results.Count == 0;
    }
}
