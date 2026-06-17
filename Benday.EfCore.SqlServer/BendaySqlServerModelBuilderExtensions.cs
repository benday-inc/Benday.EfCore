using Benday.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer;

/// <summary>
/// <see cref="ModelBuilder"/> extensions that apply SQL Server-specific model
/// configuration for the Benday.EfCore base types.
/// </summary>
public static class BendaySqlServerModelBuilderExtensions
{
    /// <summary>
    /// Maps the <see cref="CoreFieldsEntityBase.Timestamp"/> property as a SQL
    /// Server <c>rowversion</c> optimistic-concurrency token for every entity
    /// that derives from <see cref="CoreFieldsEntityBase"/>. This replaces the
    /// <c>[Timestamp]</c> attribute that the provider-agnostic base type no
    /// longer carries.
    ///
    /// Call once at the end of <c>OnModelCreating</c>, after your entities have
    /// been added to the model.
    /// </summary>
    public static ModelBuilder ApplyBendaySqlServerConcurrency(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var coreFieldsEntities = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(CoreFieldsEntityBase).IsAssignableFrom(entityType.ClrType));

        foreach (var entityType in coreFieldsEntities)
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(CoreFieldsEntityBase.Timestamp))
                .IsRowVersion();
        }

        return modelBuilder;
    }
}
