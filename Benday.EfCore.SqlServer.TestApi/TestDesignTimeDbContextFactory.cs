using Benday.EfCore.SqlServer.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.TestApi;

/// <summary>
/// Design-time factory so the EF Core CLI (<c>dotnet ef</c>) can construct a
/// <see cref="TestDbContext"/> for generating migrations. Connection-string
/// loading (from appsettings.json) and SQL Server wiring are inherited from
/// <see cref="SqlServerDesignTimeDbContextFactory{TContext}"/>.
/// </summary>
public class TestDesignTimeDbContextFactory
    : SqlServerDesignTimeDbContextFactory<TestDbContext>
{
    /// <inheritdoc />
    protected override TestDbContext Create(DbContextOptions<TestDbContext> options)
    {
        return new TestDbContext(options);
    }
}
