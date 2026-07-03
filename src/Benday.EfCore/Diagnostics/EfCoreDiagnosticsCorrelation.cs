namespace Benday.EfCore.Diagnostics;

/// <summary>
/// Ambient (async-local) correlation used to attribute an intercepted
/// command back to the repository operation that triggered it.
///
/// <para>
/// EF Core's <c>DbCommandInterceptor</c> fires below the repository, at the
/// ADO.NET command layer, so it has no direct knowledge of the calling
/// method. The repository base pushes a scope with
/// <see cref="Push"/> (e.g. "PersonRepository.SaveAsync") around each
/// operation; the interceptor reads <see cref="Current"/> and copies it
/// into <see cref="EfCoreQueryDiagnostics.Source"/>.
/// </para>
/// <para>
/// This complements <c>TagWith</c>: tags only ride on queries
/// (<see cref="IQueryable{T}"/>), so the write path — the INSERT/UPDATE/DELETE
/// from SaveChanges — is attributed through this correlation instead.
/// </para>
/// </summary>
public static class EfCoreDiagnosticsCorrelation
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>
    /// The source description for the current async context, or null when
    /// no scope is active.
    /// </summary>
    public static string? Current => _current.Value;

    /// <summary>
    /// Sets the ambient source description and returns a disposable that
    /// restores the previous value. Scopes nest correctly: disposing
    /// restores whatever was current when <see cref="Push"/> was called.
    /// </summary>
    /// <param name="source">
    /// A short origin description, e.g. "PersonRepository.SaveAsync".
    /// </param>
    public static IDisposable Push(string? source)
    {
        var previous = _current.Value;
        _current.Value = source;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly string? _previous;
        private bool _disposed;

        public Scope(string? previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current.Value = _previous;
        }
    }
}
