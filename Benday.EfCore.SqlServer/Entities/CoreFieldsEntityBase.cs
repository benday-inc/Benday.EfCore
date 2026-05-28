using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Benday.EfCore.SqlServer.Entities;

/// <summary>
/// Entity base class with audit fields (CreatedBy, CreatedDate,
/// LastModifiedBy, LastModifiedDate) and optimistic concurrency
/// via a [Timestamp] column.
///
/// Column ordering places audit fields after the entity's own columns
/// so they sort to the end of the table in the database.
/// </summary>
public abstract class CoreFieldsEntityBase : EntityBase
{
    /// <summary>
    /// Application-defined status value for the entity.
    /// </summary>
    [Column(Order = 500)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Username that created the entity. Set on insert.
    /// </summary>
    [Column(Order = 510)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of when the entity was created. Set on insert.
    /// </summary>
    [Column(Order = 520)]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Username that last modified the entity. Set on every save.
    /// </summary>
    [Column(Order = 530)]
    public string LastModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the last modification. Set on every save.
    /// </summary>
    [Column(Order = 540)]
    public DateTime LastModifiedDate { get; set; }

    /// <summary>
    /// Optimistic concurrency token managed by SQL Server (rowversion).
    /// </summary>
    [Timestamp]
    [Column(Order = 550)]
    public byte[]? Timestamp { get; set; }
}
