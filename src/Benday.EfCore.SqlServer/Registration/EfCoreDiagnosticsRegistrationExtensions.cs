using Benday.EfCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Benday.EfCore.Registration;

/// <summary>
/// Query-diagnostics registration extensions for
/// <see cref="EfCoreRegistrationHelper{TDbContext}"/>. Modeled after the
/// <c>WithQueryLogSink</c> / <c>ConfigureDiagnostics</c> surface in
/// Benday.CosmosDb.
/// </summary>
public static class EfCoreDiagnosticsRegistrationExtensions
{
    /// <summary>
    /// Turns on EF Core query diagnostics: registers the
    /// <see cref="EfCoreDiagnosticsCommandInterceptor"/> (as an
    /// <see cref="IInterceptor"/>, which <see cref="EfCoreRegistrationHelper{TDbContext}.RegisterDbContext"/>
    /// wires into the DbContext options), a default
    /// <see cref="NoOpEfCoreQueryLogSink"/> sink if none is configured, and
    /// the supplied <see cref="EfCoreQueryDiagnosticsOptions"/>.
    ///
    /// <para>
    /// Pair with <see cref="WithQueryLogSink{TDbContext, TSink}"/> (or the
    /// instance overload) to send the captured events somewhere — for
    /// example <see cref="FileEfCoreQueryLogSink"/>. Call order does not
    /// matter.
    /// </para>
    /// </summary>
    public static EfCoreRegistrationHelper<TDbContext> WithQueryDiagnostics<TDbContext>(
        this EfCoreRegistrationHelper<TDbContext> helper,
        Action<EfCoreQueryDiagnosticsOptions>? configure = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(helper);

        var options = new EfCoreQueryDiagnosticsOptions();
        configure?.Invoke(options);

        helper.Services.AddSingleton(options);
        helper.Services.TryAddSingleton<IEfCoreQueryLogSink>(NoOpEfCoreQueryLogSink.Instance);
        helper.Services.AddSingleton<IInterceptor, EfCoreDiagnosticsCommandInterceptor>();

        return helper;
    }

    /// <summary>
    /// Registers the query-log sink type as a singleton. The last sink
    /// registered wins, so this overrides the default
    /// <see cref="NoOpEfCoreQueryLogSink"/> regardless of call order relative
    /// to <see cref="WithQueryDiagnostics{TDbContext}"/>.
    /// </summary>
    public static EfCoreRegistrationHelper<TDbContext> WithQueryLogSink<TDbContext, TSink>(
        this EfCoreRegistrationHelper<TDbContext> helper)
        where TDbContext : DbContext
        where TSink : class, IEfCoreQueryLogSink
    {
        ArgumentNullException.ThrowIfNull(helper);
        helper.Services.AddSingleton<IEfCoreQueryLogSink, TSink>();
        return helper;
    }

    /// <summary>
    /// Registers a pre-built query-log sink instance as a singleton. Handy
    /// for a capturing sink in tests.
    /// </summary>
    public static EfCoreRegistrationHelper<TDbContext> WithQueryLogSink<TDbContext>(
        this EfCoreRegistrationHelper<TDbContext> helper,
        IEfCoreQueryLogSink sink)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(sink);
        helper.Services.AddSingleton(sink);
        return helper;
    }
}
