namespace Benday.EfCore.Diagnostics;

/// <summary>
/// Behavior options for the <c>EfCoreDiagnosticsCommandInterceptor</c>.
/// Registered as a singleton and configured via
/// <c>EfCoreRegistrationHelper.WithQueryDiagnostics(...)</c>.
/// </summary>
public sealed class EfCoreQueryDiagnosticsOptions
{
    /// <summary>
    /// Commands whose execution duration is at or over this value are
    /// flagged with <see cref="EfCoreQueryDiagnostics.ExceededThreshold"/>.
    /// This replaces Cosmos's RU cost as the primary "expensive query"
    /// signal. Defaults to 200 ms.
    /// </summary>
    public TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// When true, command parameter values are copied into
    /// <see cref="EfCoreQueryDiagnostics.Parameters"/>. Off by default:
    /// parameter values frequently contain personal or otherwise sensitive
    /// data, so opting in is deliberate — the same posture as EF Core's
    /// <c>EnableSensitiveDataLogging</c>.
    /// </summary>
    public bool CaptureParameters { get; set; }
}
