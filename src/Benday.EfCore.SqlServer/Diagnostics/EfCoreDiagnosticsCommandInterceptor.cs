using System.Data.Common;
using System.IO;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Benday.EfCore.Diagnostics;

/// <summary>
/// Times every EF Core database command and forwards a structured
/// <see cref="EfCoreQueryDiagnostics"/> event to the configured
/// <see cref="IEfCoreQueryLogSink"/>.
///
/// <para>
/// This is the EF Core analog of "the repository owning query execution"
/// in Benday.CosmosDb. Because EF Core intercepts at the ADO.NET command
/// layer — below the repository — it captures every command, including
/// ad-hoc LINQ that never went through a repository method. Attribution
/// back to a repository operation comes from <c>TagWith</c> comments
/// (surfaced in <see cref="EfCoreQueryDiagnostics.Tags"/>) and the ambient
/// <see cref="EfCoreDiagnosticsCorrelation"/> scope
/// (<see cref="EfCoreQueryDiagnostics.Source"/>).
/// </para>
/// <para>
/// Registering this interceptor (via
/// <c>EfCoreRegistrationHelper.WithQueryDiagnostics(...)</c>) is itself the
/// opt-in: when it is not registered there is zero overhead.
/// </para>
/// </summary>
public sealed class EfCoreDiagnosticsCommandInterceptor : DbCommandInterceptor
{
    private readonly IEfCoreQueryLogSink _sink;
    private readonly EfCoreQueryDiagnosticsOptions _options;

    /// <summary>
    /// Creates the interceptor over the supplied sink and options.
    /// </summary>
    /// <param name="sink">Destination for diagnostics events.</param>
    /// <param name="options">Threshold and capture behavior.</param>
    public EfCoreDiagnosticsCommandInterceptor(
        IEfCoreQueryLogSink sink,
        EfCoreQueryDiagnosticsOptions options)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Emit(EfCoreQueryEventKind.Reader, command, eventData.Duration, resultCount: 0);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Emit(EfCoreQueryEventKind.Reader, command, eventData.Duration, resultCount: 0);
        return new ValueTask<DbDataReader>(result);
    }

    /// <inheritdoc />
    public override object? ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Emit(EfCoreQueryEventKind.Scalar, command, eventData.Duration, ScalarCount(result));
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        Emit(EfCoreQueryEventKind.Scalar, command, eventData.Duration, ScalarCount(result));
        return new ValueTask<object?>(result);
    }

    /// <inheritdoc />
    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Emit(EfCoreQueryEventKind.NonQuery, command, eventData.Duration, resultCount: result);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        Emit(EfCoreQueryEventKind.NonQuery, command, eventData.Duration, resultCount: result);
        return new ValueTask<int>(result);
    }

    private static int ScalarCount(object? result) =>
        result is null || result is DBNull ? 0 : 1;

    private void Emit(
        EfCoreQueryEventKind kind, DbCommand command, TimeSpan duration, int resultCount)
    {
        var commandText = command.CommandText ?? string.Empty;
        var tags = ExtractTags(commandText);

        var diagnostics = new EfCoreQueryDiagnostics
        {
            EventKind = kind,
            Timestamp = DateTimeOffset.UtcNow,
            CommandText = commandText,
            Tags = tags,
            Parameters = _options.CaptureParameters ? ExtractParameters(command.Parameters) : null,
            Duration = duration,
            ResultCount = resultCount,
            ExceededThreshold = duration >= _options.SlowQueryThreshold,
            // Writes carry an ambient correlation source (they can't be tagged);
            // reads fall back to their TagWith tag. This is what lets a custom
            // read method get Source attribution from a single Tag(...) call.
            Source = EfCoreDiagnosticsCorrelation.Current
                ?? (tags.Count > 0 ? tags[0] : null)
        };

        try
        {
            _sink.Record(diagnostics);
        }
        catch
        {
            // A broken sink must never break a query.
        }
    }

    /// <summary>
    /// Parses leading <c>--</c> comment lines (emitted by <c>TagWith</c>)
    /// off the command text. EF Core renders each query tag as a SQL comment
    /// preceding the statement; there is no structured tag collection at the
    /// command-interception layer, so we read them back out of the text.
    /// </summary>
    private static IReadOnlyList<string> ExtractTags(string commandText)
    {
        if (string.IsNullOrEmpty(commandText) ||
            commandText.IndexOf("--", StringComparison.Ordinal) < 0)
        {
            return [];
        }

        var tags = new List<string>();

        using var reader = new StringReader(commandText);
        for (string? line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("--", StringComparison.Ordinal))
            {
                tags.Add(trimmed[2..].Trim());
            }
            else if (trimmed.Length > 0)
            {
                // First line of real SQL — the tag block is over.
                break;
            }
            // Blank lines between the tag block and the SQL are skipped.
        }

        return tags.Count == 0 ? [] : tags;
    }

    private static IReadOnlyDictionary<string, object?> ExtractParameters(
        DbParameterCollection parameters)
    {
        var result = new Dictionary<string, object?>(parameters.Count);
        foreach (DbParameter parameter in parameters)
        {
            result[parameter.ParameterName] =
                parameter.Value is DBNull ? null : parameter.Value;
        }
        return result;
    }
}
