namespace Benday.EfCore.Diagnostics;

/// <summary>
/// An <see cref="IEfCoreQueryLogSink"/> that discards every event. This is
/// the default sink, registered via <c>TryAddSingleton</c> when diagnostics
/// are enabled so the interceptor always has a sink to resolve even when the
/// consumer has not configured one.
/// </summary>
public sealed class NoOpEfCoreQueryLogSink : IEfCoreQueryLogSink
{
    /// <summary>
    /// The shared singleton instance.
    /// </summary>
    public static NoOpEfCoreQueryLogSink Instance { get; } = new();

    private NoOpEfCoreQueryLogSink() { }

    /// <inheritdoc />
    public void Record(EfCoreQueryDiagnostics diagnostics) { }
}
