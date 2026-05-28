using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Benday.EfCore.SqlServer.TestApi;

/// <summary>
/// Design-time factory so the EF Core CLI (<c>dotnet ef</c>) can construct a
/// <see cref="TestDbContext"/> for generating migrations. Reads the connection
/// string named "default" from appsettings.json.
/// </summary>
public class TestDesignTimeDbContextFactory :
    IDesignTimeDbContextFactory<TestDbContext>
{
    /// <summary>Creates a context using the ambient environment and base directory.</summary>
    public static TestDbContext Create()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var basePath = AppContext.BaseDirectory;

        return Create(basePath, environmentName);
    }

    /// <inheritdoc />
    public TestDbContext CreateDbContext(string[] args)
    {
        return Create(
            Directory.GetCurrentDirectory(),
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    }

    private static TestDbContext Create(string basePath, string? environmentName)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddJsonFile("appsettings.unversioned.json", optional: true)
            .AddEnvironmentVariables();

        var config = builder.Build();

        var connectionString = config.GetConnectionString("default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Could not find a connection string named 'default'.");
        }

        return Create(connectionString);
    }

    private static TestDbContext Create(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException(
                $"{nameof(connectionString)} is null or empty.",
                nameof(connectionString));
        }

        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TestDbContext(optionsBuilder.Options);
    }
}
