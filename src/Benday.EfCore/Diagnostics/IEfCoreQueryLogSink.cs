namespace Benday.EfCore.Diagnostics;

/// <summary>
/// A sink for EF Core query diagnostics. Implementations receive an
/// <see cref="EfCoreQueryDiagnostics"/> event for every database command
/// the <c>EfCoreDiagnosticsCommandInterceptor</c> captures — readers,
/// scalars, and non-queries.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are registered in the DI container (typically as
/// singletons) and injected into the interceptor. A default
/// <see cref="NoOpEfCoreQueryLogSink"/> is registered automatically when
/// diagnostics are enabled, so consumers only register a sink when they
/// want non-default behavior (for example
/// <see cref="FileEfCoreQueryLogSink"/>).
/// </para>
/// <para>
/// The <see cref="Record"/> method is synchronous and fire-and-forget
/// from the interceptor's perspective. Sinks that need async I/O (file
/// writes, network calls) should buffer internally and flush from a
/// background worker — the interceptor will not await the sink and will
/// not retry on failure.
/// </para>
/// <para>
/// Exceptions thrown from <see cref="Record"/> are caught and suppressed
/// by the interceptor. A broken sink must never prevent a query from
/// completing.
/// </para>
/// </remarks>
public interface IEfCoreQueryLogSink
{
    /// <summary>
    /// Records a diagnostics event. Called synchronously on the thread
    /// that executed the command.
    /// </summary>
    /// <param name="diagnostics">
    /// The diagnostics payload for this event. Never null.
    /// </param>
    void Record(EfCoreQueryDiagnostics diagnostics);
}
