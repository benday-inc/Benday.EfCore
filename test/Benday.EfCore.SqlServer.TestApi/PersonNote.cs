using System.ComponentModel.DataAnnotations;
using Benday.EfCore.Entities;

namespace Benday.EfCore.SqlServer.TestApi;

/// <summary>
/// Child entity owned by a <see cref="Person"/>. Derives from
/// <see cref="EntityBase"/> so it carries Id + IsMarkedForDelete and
/// participates in the dependent-entity save/delete lifecycle.
/// </summary>
public class PersonNote : EntityBase
{
    /// <summary>Foreign key to the owning person.</summary>
    public int PersonId { get; set; }

    /// <summary>Navigation back to the owning person.</summary>
    public Person? Person { get; set; }

    /// <summary>The note text.</summary>
    [Required]
    public string NoteText { get; set; } = string.Empty;
}
