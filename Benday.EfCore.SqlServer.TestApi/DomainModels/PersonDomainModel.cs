using System.ComponentModel.DataAnnotations;
using Benday.EfCore.SqlServer.DomainModels;

namespace Benday.EfCore.SqlServer.TestApi.DomainModels;

/// <summary>
/// Business-logic representation of a person. Lives on the domain side of the
/// adapter boundary — EF Core never sees this type. Carries audit fields and
/// the concurrency token from <see cref="CoreFieldsDomainModelBase"/>.
/// DataAnnotations here drive <c>DefaultValidatorStrategy</c>.
/// </summary>
public class PersonDomainModel : CoreFieldsDomainModelBase
{
    /// <summary>The person's first name. Required.</summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The person's last name. Required.</summary>
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Child notes for this person.</summary>
    public List<PersonNoteDomainModel> Notes { get; set; } = new();
}

/// <summary>
/// Business-logic representation of a person note. Plain domain model
/// (Id only) mirroring the <c>PersonNote</c> entity.
/// </summary>
public class PersonNoteDomainModel : DomainModelBase
{
    /// <summary>The note text. Required.</summary>
    [Required]
    public string NoteText { get; set; } = string.Empty;
}
