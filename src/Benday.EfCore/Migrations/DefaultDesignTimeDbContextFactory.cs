using Benday.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Benday.EfCore.Migrations;

/// <summary>
/// Provider-agnostic design-time DbContext factory base so the EF Core CLI
/// (<c>dotnet ef</c>) can construct a context for generating migrations. Reads a
/// named connection string (default: "default") from appsettings.json plus
/// environment-specific overrides and environment variables.
///
/// Provider packages supply the actual provider wiring by overriding
/// <see cref="ConfigureProvider"/> (for example,
/// <c>SqlServerDesignTimeDbContextFactory</c> in Benday.EfCore.SqlServer calls
/// <c>UseSqlServer</c>). Concrete factories override
/// <see cref="Create(DbContextOptions{TContext})"/> to construct the context.
/// </summary>
public abstract class DefaultDesignTimeDbContextFactory<TContext> :
    IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>Name of the connection string to read. Defaults to "default".</summary>
    protected virtual string GetConnectionStringName()
    {
        return "default";
    }

    /// <inheritdoc />
    public TContext CreateDbContext(string[] args)
    {
        return Create(
            Directory.GetCurrentDirectory(),
            Environment.GetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT").SafeToString());
    }

    private TContext Create(string basePath, string environmentName)
    {
        var config = GetConfiguration(basePath, environmentName);

        var connectionStringName = GetConnectionStringName();

        var connectionString = config.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Could not find a connection string named '{connectionStringName}'.");
        }

        return Create(connectionString);
    }

    /// <summary>
    /// Builds configuration from appsettings.json, an optional
    /// environment-specific override, an optional unversioned override, and
    /// environment variables.
    /// </summary>
    protected virtual IConfigurationRoot GetConfiguration(string basePath, string environmentName)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddJsonFile("appsettings.unversioned.json", optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// Creates the context from a connection string, applying the EF Core
    /// provider via <see cref="ConfigureProvider"/>.
    /// </summary>
    protected virtual TContext Create(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException(
                $"{nameof(connectionString)} is null or empty.",
                nameof(connectionString));
        }

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        ConfigureProvider(optionsBuilder, connectionString);

        return Create(optionsBuilder.Options);
    }

    /// <summary>
    /// Apply the EF Core provider to the options builder (for example,
    /// <c>optionsBuilder.UseSqlServer(connectionString)</c>). Implemented by a
    /// provider-specific base class.
    /// </summary>
    protected abstract void ConfigureProvider(
        DbContextOptionsBuilder<TContext> optionsBuilder, string connectionString);

    /// <summary>Construct the concrete context from the configured options.</summary>
    protected abstract TContext Create(DbContextOptions<TContext> options);
}
