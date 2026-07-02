using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.EfCore.Registration;

/// <summary>
/// Extension methods for IServiceCollection to make registration clean.
/// </summary>
public static class EfCoreRegistrationExtensions
{
    /// <summary>
    /// Add Benday EF Core architecture services using a fluent configuration.
    ///
    /// <code>
    /// services.AddBendayEfCore&lt;MyDbContext&gt;(options =&gt;
    /// {
    ///     options.UseConnectionString(connectionString);
    ///     options.RegisterDbContext();
    ///     options.RegisterRepository&lt;IPersonRepo, SqlPersonRepo&gt;();
    ///     options.RegisterAdapter&lt;PersonAdapter&gt;();
    ///     options.RegisterService&lt;IPersonService, PersonService&gt;();
    /// });
    /// </code>
    /// </summary>
    public static IServiceCollection AddBendayEfCore<TDbContext>(
        this IServiceCollection services,
        Action<EfCoreRegistrationHelper<TDbContext>> configure)
        where TDbContext : DbContext
    {
        var helper = new EfCoreRegistrationHelper<TDbContext>(services);
        configure(helper);
        return services;
    }
}
