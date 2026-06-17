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
    /// Maps the <see cref="CoreFieldsEntityBase{TIdentity}.Timestamp"/> property as a SQL
    /// Server <c>rowversion</c> optimistic-concurrency token for every entity
    /// that derives from <see cref="CoreFieldsEntityBase{TIdentity}"/> (including the
    /// non-generic <see cref="CoreFieldsEntityBase"/> int shim). This replaces the
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
            .Where(entityType => IsCoreFieldsEntity(entityType.ClrType));

        foreach (var entityType in coreFieldsEntities)
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(CoreFieldsEntityBase<int>.Timestamp))
                .IsRowVersion();
        }

        return modelBuilder;
    }

    /// <summary>
    /// Walks the base-type chain looking for the open generic
    /// <see cref="CoreFieldsEntityBase{TIdentity}"/>. This matches both int
    /// consumers (via the <see cref="CoreFieldsEntityBase"/> shim) and consumers
    /// using any other identity type (e.g. <c>CoreFieldsEntityBase&lt;Guid&gt;</c>).
    /// </summary>
    private static bool IsCoreFieldsEntity(Type type)
    {
        var current = type;
        while (current != null)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(CoreFieldsEntityBase<>))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}
