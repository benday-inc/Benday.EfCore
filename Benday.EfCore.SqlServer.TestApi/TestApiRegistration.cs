using Benday.EfCore.SqlServer.Registration;
using Benday.EfCore.SqlServer.TestApi.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;
using Benday.EfCore.SqlServer.TestApi.Repositories;
using Benday.EfCore.SqlServer.TestApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.EfCore.SqlServer.TestApi;

/// <summary>
/// Demonstrates wiring the worked example with the fluent registration helper.
/// </summary>
public static class TestApiRegistration
{
    /// <summary>
    /// Registers the <see cref="TestDbContext"/> plus the Person aggregate
    /// (repository + adapter + default validator + service) and a username
    /// provider, using <c>AddBendayEfCore</c>.
    /// </summary>
    public static IServiceCollection AddTestApi(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddBendayEfCore<TestDbContext>(options =>
        {
            options.UseConnectionString(connectionString);
            options.RegisterDbContext();

            options.RegisterUsernameProvider<EnvironmentUsernameProvider>();

            options.RegisterAggregate<
                IPersonRepository, SqlPersonRepository,
                PersonAdapter,
                PersonDomainModel,
                IPersonService, PersonService>();
        });

        return services;
    }
}
