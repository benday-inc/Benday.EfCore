using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Benday.EfCore.Diagnostics;

/// <summary>
/// An <see cref="IEfCoreQueryLogSink"/> that appends each diagnostics event
/// as a single line of JSON (NDJSON / JSON Lines) to a file.
/// </summary>
/// <remarks>
/// <para>
/// Events are handed to an in-memory queue from <see cref="Record"/> and
/// written to disk by a single background thread, so the query-execution
/// path is never blocked on file I/O. Dispose the sink (or let the host
/// shut it down) to flush any queued events.
/// </para>
/// <para>
/// If the queue fills past <see cref="EfCoreFileLogSinkOptions.QueueCapacity"/>
/// (default 10,000), new events are dropped. This keeps a stuck disk from
/// growing memory without bound.
/// </para>
/// </remarks>
public sealed class FileEfCoreQueryLogSink : IEfCoreQueryLogSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private readonly string _filePath;
    private readonly BlockingCollection<EfCoreQueryDiagnostics> _queue;
    private readonly Thread _worker;
    private readonly CancellationTokenSource _shutdownCts = new();
    private int _droppedCount;
    private bool _disposed;

    /// <summary>Initializes a new sink that writes NDJSON to the specified file path.</summary>
    /// <param name="filePath">Destination file path for the query log.</param>
    public FileEfCoreQueryLogSink(string filePath)
        : this(new EfCoreFileLogSinkOptions { FilePath = filePath })
    {
    }

    /// <summary>Initializes a new sink using default options (including the default file path).</summary>
    public FileEfCoreQueryLogSink()
        : this(new EfCoreFileLogSinkOptions())
    {
    }

    /// <summary>
    /// DI-friendly constructor. Resolves options via the
    /// <see cref="IOptions{TOptions}"/> pattern so consumers can register
    /// <see cref="EfCoreFileLogSinkOptions"/> with
    /// <c>services.Configure&lt;EfCoreFileLogSinkOptions&gt;(...)</c> or bind
    /// from <c>IConfiguration</c>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public FileEfCoreQueryLogSink(IOptions<EfCoreFileLogSinkOptions> options)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>
    /// Initializes a new sink from the given options, creating the target directory if needed and
    /// starting the background writer thread.
    /// </summary>
    /// <param name="options">File path and queue-capacity settings for the sink.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public FileEfCoreQueryLogSink(EfCoreFileLogSinkOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _filePath = string.IsNullOrWhiteSpace(options.FilePath)
            ? EfCoreFileLogSinkOptions.GetDefaultFilePath()
            : options.FilePath;
        _queue = new BlockingCollection<EfCoreQueryDiagnostics>(options.QueueCapacity);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        _worker = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = nameof(FileEfCoreQueryLogSink)
        };
        _worker.Start();
    }

    /// <summary>
    /// Number of events dropped because the queue was full. Useful for
    /// surfacing back-pressure in tests or health checks.
    /// </summary>
    public int DroppedCount => Volatile.Read(ref _droppedCount);

    /// <inheritdoc />
    public void Record(EfCoreQueryDiagnostics diagnostics)
    {
        if (diagnostics is null) return;
        if (_queue.IsAddingCompleted) return;

        if (_queue.TryAdd(diagnostics) == false)
        {
            Interlocked.Increment(ref _droppedCount);
        }
    }

    private void ProcessQueue()
    {
        try
        {
            foreach (var diagnostics in _queue.GetConsumingEnumerable(_shutdownCts.Token))
            {
                try
                {
                    var line = Serialize(diagnostics);
                    File.AppendAllText(_filePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // A broken sink must not prevent queries from completing.
                    // Because writes happen off-thread here, we swallow them
                    // ourselves rather than relying on the interceptor's guard.
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string Serialize(EfCoreQueryDiagnostics d)
    {
        var payload = new Dictionary<string, object?>
        {
            ["eventKind"] = d.EventKind.ToString(),
            ["timestamp"] = d.Timestamp.ToUniversalTime().ToString("o"),
            ["source"] = d.Source,
            ["tags"] = d.Tags,
            ["commandText"] = d.CommandText,
            ["parameters"] = d.Parameters,
            ["durationMs"] = d.Duration.TotalMilliseconds,
            ["resultCount"] = d.ResultCount,
            ["exceededThreshold"] = d.ExceededThreshold
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    /// <summary>
    /// Stops accepting new events and blocks until the background writer
    /// has flushed everything currently queued.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _queue.CompleteAdding();
        _worker.Join(TimeSpan.FromSeconds(5));
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        _queue.Dispose();
    }
}
