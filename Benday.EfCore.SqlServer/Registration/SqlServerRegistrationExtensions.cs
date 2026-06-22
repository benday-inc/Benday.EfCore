using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Benday.EfCore.Registration;

/// <summary>
/// SQL Server-specific conveniences layered on top of the provider-agnostic
/// <see cref="EfCoreRegistrationHelper{TDbContext}"/>.
/// </summary>
public static class SqlServerRegistrationExtensions
{
    /// <summary>
    /// Configure the DbContext to use SQL Server with the supplied connection
    /// string. Sugar over <see cref="EfCoreRegistrationHelper{TDbContext}.ConfigureDbContext"/>.
    /// </summary>
    public static EfCoreRegistrationHelper<TDbContext> UseConnectionString<TDbContext>(
        this EfCoreRegistrationHelper<TDbContext> helper, string connectionString)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(helper);
        return helper.ConfigureDbContext(options => options.UseSqlServer(connectionString));
    }

    /// <summary>
    /// Configure the DbContext to use SQL Server with a named connection string
    /// from configuration. Sugar over <see cref="UseConnectionString{TDbContext}(EfCoreRegistrationHelper{TDbContext}, string)"/>.
    /// </summary>
    /// <param name="helper">The registration helper.</param>
    /// <param name="configuration">The configuration to read the connection string from.</param>
    /// <param name="connectionStringName">
    /// The name of the connection string in the ConnectionStrings section. Defaults to "default".
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection string is not found or is empty/whitespace.
    /// </exception>
    public static EfCoreRegistrationHelper<TDbContext> UseConnectionString<TDbContext>(
        this EfCoreRegistrationHelper<TDbContext> helper,
        IConfiguration configuration,
        string connectionStringName = "default")
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' not found or is empty in configuration.");
        }

        return helper.UseConnectionString(connectionString);
    }
}
