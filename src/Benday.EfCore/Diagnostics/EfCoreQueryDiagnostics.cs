namespace Benday.EfCore.Diagnostics;

/// <summary>
/// Structured payload describing a single EF Core database command
/// execution. Delivered to <see cref="IEfCoreQueryLogSink"/> for every
/// command the <c>EfCoreDiagnosticsCommandInterceptor</c> observes.
///
/// This is the EF Core analog of <c>CosmosQueryDiagnostics</c>. Provider
/// concepts that don't exist in EF Core (RU charge, partition key,
/// cross-partition detection, Cosmos index metrics) are intentionally
/// absent; <see cref="ExceededThreshold"/> replaces RU cost as the
/// headline "this one was expensive" signal.
/// </summary>
public sealed class EfCoreQueryDiagnostics
{
    /// <summary>
    /// Which kind of command this is. Drives how sinks route and aggregate.
    /// </summary>
    public EfCoreQueryEventKind EventKind { get; init; }

    /// <summary>
    /// When the event was captured, in UTC.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The generated SQL text, including any leading <c>TagWith</c> comment
    /// lines (which are parsed out into <see cref="Tags"/>).
    /// </summary>
    public string CommandText { get; init; } = string.Empty;

    /// <summary>
    /// Values captured from <c>TagWith(...)</c> calls on the query, parsed
    /// from the leading SQL comment lines of <see cref="CommandText"/>.
    /// Populated for reads issued through the repository base (which tags
    /// every query with "&lt;RepositoryType&gt;.&lt;Method&gt;"). Empty for
    /// commands that carry no tags — notably the INSERT/UPDATE/DELETE
    /// statements from SaveChanges, which cannot be tagged with
    /// <c>TagWith</c>; those are attributed via <see cref="Source"/> instead.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Command parameters. Null unless
    /// <see cref="EfCoreQueryDiagnosticsOptions.CaptureParameters"/> is
    /// enabled — parameter values can contain sensitive data, mirroring
    /// EF Core's own <c>EnableSensitiveDataLogging</c> stance.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// Wall-clock execution time, taken from the interceptor's
    /// <c>CommandExecutedEventData.Duration</c> (no manual stopwatch needed).
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Row count. For <see cref="EfCoreQueryEventKind.NonQuery"/> this is
    /// the number of rows affected; for <see cref="EfCoreQueryEventKind.Scalar"/>
    /// it is 1 for a non-null value or 0 otherwise. For
    /// <see cref="EfCoreQueryEventKind.Reader"/> it is 0 — the row count is
    /// not known at interception time because the reader has not yet been
    /// consumed.
    /// </summary>
    public int ResultCount { get; init; }

    /// <summary>
    /// True when <see cref="Duration"/> met or exceeded the configured
    /// <see cref="EfCoreQueryDiagnosticsOptions.SlowQueryThreshold"/>.
    /// This is the EF Core stand-in for Cosmos's RU-cost signal.
    /// </summary>
    public bool ExceededThreshold { get; init; }

    /// <summary>
    /// Optional origin tag for the command, e.g. "PersonRepository.SaveAsync".
    /// Populated from the ambient <c>EfCoreDiagnosticsCorrelation</c> scope the
    /// repository base pushes around each operation. Unlike <see cref="Tags"/>,
    /// this also covers the write path (SaveChanges/Delete). Null when no
    /// correlation scope was active.
    /// </summary>
    public string? Source { get; init; }
}
