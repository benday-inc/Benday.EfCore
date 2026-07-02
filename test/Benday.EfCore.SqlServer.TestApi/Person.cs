using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Benday.EfCore.Entities;

namespace Benday.EfCore.SqlServer.TestApi;

/// <summary>
/// Aggregate root entity. Owns a collection of <see cref="PersonNote"/> children
/// and exposes them as a dependent entity collection so the repository handles
/// their save/delete lifecycle. Inherits audit fields and a [Timestamp]
/// concurrency token from <see cref="CoreFieldsEntityBase"/>.
/// </summary>
[Table("Person")]
public class Person : CoreFieldsEntityBase
{
    /// <summary>The person's first name.</summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The person's last name.</summary>
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Child notes owned by this person.</summary>
    public List<PersonNote> Notes { get; set; } = new();

    /// <summary>
    /// Exposes <see cref="Notes"/> as a dependent collection so the repository
    /// prunes notes flagged for delete during save.
    /// </summary>
    public override IList<IDependentEntityCollection>? GetDependentEntities()
    {
        return new List<IDependentEntityCollection>
        {
            new DependentEntityCollection<PersonNote>(Notes)
        };
    }
}
