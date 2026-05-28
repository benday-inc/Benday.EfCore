using Benday.Common.Testing;
using Benday.EfCore.SqlServer.TestApi;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.IntegrationTests;

/// <summary>
/// Base class for SQL Server-backed integration tests. Provides a known
/// connection string, fresh <see cref="TestDbContext"/> instances, and a
/// per-test "skip if the database is unavailable, otherwise migrate + clean"
/// setup so the suite is a no-op (skipped) when SQL Server isn't running
/// rather than a wall of failures.
///
/// Run <c>./start-local-dev.ps1</c> to start SQL Server in Docker first.
/// </summary>
public abstract class IntegrationTestBase : TestClassBase
{
    /// <summary>Connection string for the local dev SQL Server container.</summary>
    protected const string ConnectionString =
        "Server=localhost; Database=benday-efcore-sqlserver; User Id=sa; Password=Pa$$word; TrustServerCertificate=True";

    private const string ServerProbeConnectionString =
        "Server=localhost; Database=master; User Id=sa; Password=Pa$$word; TrustServerCertificate=True; Connect Timeout=3";

    private static bool? _databaseAvailable;

    protected IntegrationTestBase(ITestOutputHelper output) : base(output) { }

    /// <summary>Creates a fresh context against the local SQL Server.</summary>
    protected static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>
    /// Skips the calling test when SQL Server isn't reachable; otherwise applies
    /// migrations and clears existing data so each test starts from a clean slate.
    /// </summary>
    protected static async Task EnsureCleanDatabaseAsync()
    {
        Assert.SkipUnless(DatabaseIsAvailable(),
            "SQL Server is not available at localhost. Run ./start-local-dev.ps1 to start it.");

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();

        context.PersonNotes.RemoveRange(context.PersonNotes);
        context.Persons.RemoveRange(context.Persons);
        await context.SaveChangesAsync();
    }

    private static bool DatabaseIsAvailable()
    {
        if (_databaseAvailable.HasValue)
        {
            return _databaseAvailable.Value;
        }

        try
        {
            using var connection = new SqlConnection(ServerProbeConnectionString);
            connection.Open();
            _databaseAvailable = true;
        }
        catch
        {
            _databaseAvailable = false;
        }

        return _databaseAvailable.Value;
    }
}
