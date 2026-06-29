using Benday.EfCore.ServiceLayers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.EfCore.Registration;

/// <summary>
/// Fluent registration helper for wiring up the repository + adapter +
/// service layer architecture with DI. Modeled after CosmosRegistrationHelper.
///
/// Usage:
/// <code>
/// services.AddBendayEfCore&lt;MyDbContext&gt;(options =&gt;
/// {
///     options.UseConnectionString(connectionString);
///     options.RegisterDbContext();
///
///     options.RegisterAggregate&lt;
///         IPersonRepository, SqlPersonRepository,
///         PersonAdapter,
///         PersonDomainModel,
///         IPersonService, PersonService&gt;();
/// });
/// </code>
/// </summary>
public class EfCoreRegistrationHelper<TDbContext> where TDbContext : DbContext
{
    private readonly IServiceCollection _services;
    private Action<DbContextOptionsBuilder>? _dbContextOptions;

    /// <summary>
    /// Creates the registration helper over the supplied service collection.
    /// </summary>
    public EfCoreRegistrationHelper(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Configure DbContext options directly. Provider packages layer
    /// convenience methods on top of this (for example, the
    /// <c>UseConnectionString</c> extension in Benday.EfCore.SqlServer).
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> ConfigureDbContext(
        Action<DbContextOptionsBuilder> configure)
    {
        _dbContextOptions = configure;
        return this;
    }

    /// <summary>
    /// Register the DbContext with EF Core using the configured options.
    /// Call <see cref="ConfigureDbContext"/> (or a provider's
    /// <c>UseConnectionString</c>) first, then call this once before
    /// registering repositories and services.
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterDbContext()
    {
        if (_dbContextOptions == null)
        {
            throw new InvalidOperationException(
                "Call ConfigureDbContext(...) (or a provider's UseConnectionString(...)) " +
                "before RegisterDbContext().");
        }

        _services.AddDbContext<TDbContext>(_dbContextOptions);

        return this;
    }

    /// <summary>
    /// Register a repository (interface + implementation) as scoped.
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterRepository<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _services.AddScoped<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Register an adapter as a singleton (adapters are stateless).
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterAdapter<TAdapter>()
        where TAdapter : class
    {
        _services.AddSingleton<TAdapter>();
        return this;
    }

    /// <summary>
    /// Register a service (interface + implementation) as scoped.
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterService<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _services.AddScoped<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Register a validator strategy for a model type.
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterValidator<TModel, TValidator>()
        where TValidator : class, IValidatorStrategy<TModel>
    {
        _services.AddScoped<IValidatorStrategy<TModel>, TValidator>();
        return this;
    }

    /// <summary>
    /// Register DefaultValidatorStrategy for a model type.
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterDefaultValidator<TModel>()
    {
        _services.AddScoped<IValidatorStrategy<TModel>, DefaultValidatorStrategy<TModel>>();
        return this;
    }

    /// <summary>
    /// Register a username provider implementation.
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterUsernameProvider<TProvider>()
        where TProvider : class, IUsernameProvider
    {
        _services.AddScoped<IUsernameProvider, TProvider>();
        return this;
    }

    /// <summary>
    /// Convenience method: registers repository + adapter + service +
    /// default validator in one call for a complete aggregate.
    /// </summary>
    public EfCoreRegistrationHelper<TDbContext> RegisterAggregate<
        TRepoInterface, TRepoImplementation,
        TAdapter,
        TModel,
        TServiceInterface, TServiceImplementation>()
        where TRepoInterface : class
        where TRepoImplementation : class, TRepoInterface
        where TAdapter : class
        where TServiceInterface : class
        where TServiceImplementation : class, TServiceInterface
    {
        RegisterRepository<TRepoInterface, TRepoImplementation>();
        RegisterAdapter<TAdapter>();
        RegisterDefaultValidator<TModel>();
        RegisterService<TServiceInterface, TServiceImplementation>();

        return this;
    }
}
