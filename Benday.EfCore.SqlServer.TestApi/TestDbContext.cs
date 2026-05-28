using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.TestApi;

/// <summary>
/// EF Core context for the worked example. Maps the <see cref="Person"/>
/// aggregate and its <see cref="PersonNote"/> children with a required,
/// cascade-delete relationship.
/// </summary>
public class TestDbContext : DbContext
{
    /// <summary>Creates the context with the supplied options.</summary>
    public TestDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>Persons aggregate roots.</summary>
    public DbSet<Person> Persons => Set<Person>();

    /// <summary>Person notes (child entities).</summary>
    public DbSet<PersonNote> PersonNotes => Set<PersonNote>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Person>()
            .HasMany(p => p.Notes)
            .WithOne(n => n.Person!)
            .HasForeignKey(n => n.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
