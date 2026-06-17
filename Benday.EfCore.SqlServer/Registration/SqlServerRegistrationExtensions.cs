using Microsoft.EntityFrameworkCore;

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
}
