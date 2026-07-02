using Benday.EfCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.Migrations;

/// <summary>
/// SQL Server design-time DbContext factory base. Subclass it and implement
/// <c>Create(DbContextOptions)</c> to give the EF Core CLI (<c>dotnet ef</c>) a
/// way to construct your context for generating migrations. Connection string
/// loading is inherited from <see cref="DefaultDesignTimeDbContextFactory{TContext}"/>.
/// </summary>
public abstract class SqlServerDesignTimeDbContextFactory<TContext>
    : DefaultDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>Wires the SQL Server provider with the resolved connection string.</summary>
    protected override void ConfigureProvider(
        DbContextOptionsBuilder<TContext> optionsBuilder, string connectionString)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }
}
