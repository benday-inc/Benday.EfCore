using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Benday.EfCore.Entities;

/// <summary>
/// Non-generic int convenience shim over <see cref="CoreFieldsEntityBase{TIdentity}"/>.
/// Int consumers derive from this and keep their existing syntax unchanged.
/// </summary>
public abstract class CoreFieldsEntityBase : CoreFieldsEntityBase<int>
{
}

/// <summary>
/// Entity base class with audit fields (CreatedBy, CreatedDate,
/// LastModifiedBy, LastModifiedDate) and an optimistic concurrency
/// token (<see cref="Timestamp"/>).
///
/// The concurrency token is mapped per provider rather than via a
/// DataAnnotations attribute so this type stays provider-agnostic.
/// SQL Server consumers map it as <c>rowversion</c> by calling
/// <c>ApplyBendaySqlServerConcurrency()</c> (from Benday.EfCore.SqlServer)
/// in <c>OnModelCreating</c>.
///
/// Column ordering places audit fields after the entity's own columns
/// so they sort to the end of the table in the database.
/// </summary>
/// <typeparam name="TIdentity">The primary key type.</typeparam>
public abstract class CoreFieldsEntityBase<TIdentity> : EntityBase<TIdentity>
    where TIdentity : IEquatable<TIdentity>
{
    /// <summary>
    /// Application-defined status value for the entity.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Username that created the entity. Set on insert.
    /// </summary>
    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of when the entity was created. Set on insert.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Username that last modified the entity. Set on every save.
    /// </summary>
    [StringLength(50)]
    public string LastModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the last modification. Set on every save.
    /// </summary>
    public DateTime LastModifiedDate { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Configured per provider: SQL Server
    /// consumers map it as <c>rowversion</c> via
    /// <c>ApplyBendaySqlServerConcurrency()</c>; PostgreSQL consumers use
    /// <c>xmin</c> instead and can leave this property unmapped.
    /// </summary>
    public byte[]? Timestamp { get; set; }

    /// <summary>
    /// Sets the created and last modified audit fields for a new entity.
    /// Call this on insert. Sets CreatedBy, CreatedDate, LastModifiedBy,
    /// and LastModifiedDate to the supplied username and <see cref="DateTime.UtcNow"/>.
    /// </summary>
    /// <param name="byUsername">The username to record as creator and modifier.</param>
    public virtual void SetCreatedFields(string byUsername)
    {
        var now = DateTime.UtcNow;

        CreatedBy = byUsername;
        CreatedDate = now;
        LastModifiedBy = byUsername;
        LastModifiedDate = now;
    }

    /// <summary>
    /// Sets the last modified audit fields for an existing entity.
    /// Call this on update. Sets LastModifiedBy and LastModifiedDate
    /// to the supplied username and <see cref="DateTime.UtcNow"/>.
    /// </summary>
    /// <param name="byUsername">The username to record as modifier.</param>
    public virtual void SetLastModifiedFields(string byUsername)
    {
        LastModifiedBy = byUsername;
        LastModifiedDate = DateTime.UtcNow;
    }
}


