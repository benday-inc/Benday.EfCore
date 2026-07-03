using Benday.Common.Testing;
using Benday.EfCore.Diagnostics;
using Benday.EfCore.Registration;
using Benday.EfCore.SqlServer.TestApi;
using Benday.EfCore.SqlServer.TestApi.Repositories;
using Benday.EfCore.SqlServer.TestApi.Services;
using Benday.EfCore.SqlServer.TestApi.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.EfCore.SqlServer.IntegrationTests;

/// <summary>
/// End-to-end verification that <c>WithQueryDiagnostics</c> wires the
/// command interceptor into the DbContext and that repository operations
/// produce diagnostics events with the expected attribution:
/// reads carry <c>TagWith</c> tags, writes carry the correlation source.
/// </summary>
public class QueryDiagnosticsIntegrationTests : IntegrationTestBase
{
    public QueryDiagnosticsIntegrationTests(ITestOutputHelper output) : base(output) { }

    private static ServiceProvider BuildProvider(IEfCoreQueryLogSink sink)
    {
        var services = new ServiceCollection();

        services.AddBendayEfCore<TestDbContext>(options =>
        {
            options.UseConnectionString(ConnectionString);
            options.WithQueryLogSink(sink);
            options.WithQueryDiagnostics(o => o.CaptureParameters = true);
            options.RegisterDbContext();

            options.RegisterUsernameProvider<EnvironmentUsernameProvider>();
            options.RegisterAggregate<
                IPersonRepository, SqlPersonRepository,
                PersonAdapter,
                PersonDomainModel,
                IPersonService, PersonService>();
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Save_EmitsEvent_AttributedToRepositorySaveViaCorrelation()
    {
        await EnsureCleanDatabaseAsync();

        var sink = new CapturingEfCoreQueryLogSink();
        await using var provider = BuildProvider(sink);

        // act — resolve the DI-wired repository and save through it
        using (var scope = provider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();
            await repo.SaveAsync(new Person { FirstName = "Ada", LastName = "Lovelace" });
        }

        // assert — the write is attributed via the ambient correlation source
        // (TagWith can't tag writes). Note: because Person has an identity Id
        // and a rowversion, EF Core emits the INSERT as an INSERT...SELECT
        // executed via ExecuteReader, so the event kind is Reader, not
        // NonQuery — hence we assert on Source across any kind.
        AssertThat.IsTrue(
            sink.Events.Any(e => e.Source == "SqlPersonRepository.SaveAsync"),
            "The write should be attributed to SqlPersonRepository.SaveAsync via correlation");
    }

    [Fact]
    public async Task GetById_EmitsReaderEvent_TaggedWithRepositoryMethod()
    {
        await EnsureCleanDatabaseAsync();

        var sink = new CapturingEfCoreQueryLogSink();
        await using var provider = BuildProvider(sink);

        // arrange — seed a person
        int id;
        using (var scope = provider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();
            var person = new Person { FirstName = "Grace", LastName = "Hopper" };
            await repo.SaveAsync(person);
            id = person.Id;
        }

        sink.Clear();

        // act — read it back through a fresh scope
        using (var scope = provider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();
            await repo.GetByIdAsync(id);
        }

        // assert — the SELECT was captured with the TagWith tag parsed out,
        // and the correlation source is also populated for reads
        var readEvent = sink.Events
            .FirstOrDefault(e => e.EventKind == EfCoreQueryEventKind.Reader
                && e.Tags.Contains("SqlPersonRepository.GetByIdAsync"));

        AssertThat.IsNotNull(readEvent, "A tagged reader event should be captured");
        readEvent!.Source.ShouldEqual("SqlPersonRepository.GetByIdAsync",
            "Reads should also be attributed via correlation source");
        AssertThat.IsTrue(readEvent.CommandText.Length > 0, "Command text should be captured");
    }

    [Fact]
    public async Task ConcurrentOperations_EachEventAttributedToItsOwnOperation_NoCrossTalk()
    {
        await EnsureCleanDatabaseAsync();

        var sink = new CapturingEfCoreQueryLogSink();
        await using var provider = BuildProvider(sink);

        // arrange — seed rows to read back
        var ids = new List<int>();
        using (var scope = provider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();
            for (int i = 0; i < 5; i++)
            {
                var person = new Person { FirstName = $"Seed{i}", LastName = "Person" };
                await repo.SaveAsync(person);
                ids.Add(person.Id);
            }
        }

        sink.Clear();

        // act — fan out many concurrent reads and writes, each in its own DI
        // scope (its own DbContext, the correct way to run EF concurrently),
        // all sharing the one capturing sink.
        const int operationCount = 40;
        var tasks = new List<Task>(operationCount);
        for (int i = 0; i < operationCount; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = provider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();
                if (index % 2 == 0)
                {
                    await repo.GetByIdAsync(ids[index % ids.Count]);
                }
                else
                {
                    await repo.SaveAsync(
                        new Person { FirstName = $"Concurrent{index}", LastName = "Person" });
                }
            }));
        }
        await Task.WhenAll(tasks);

        // assert
        var events = sink.Events;
        AssertThat.IsTrue(events.Count > 0, "Concurrent operations should have produced events");

        var knownSources = new[]
        {
            "SqlPersonRepository.GetByIdAsync",
            "SqlPersonRepository.SaveAsync"
        };

        foreach (var capturedEvent in events)
        {
            // No leaked / garbage source values under concurrency.
            AssertThat.IsTrue(
                capturedEvent.Source is not null && knownSources.Contains(capturedEvent.Source),
                $"Every event should carry a known source; got '{capturedEvent.Source}'");

            // The anti-cross-talk proof: the TagWith tag is baked into the SQL
            // command text (immune to threading), while Source comes from the
            // AsyncLocal correlation (the thing under test). If AsyncLocal bled
            // across concurrent flows, a read's Source would stop matching the
            // tag on its own command. For every tagged event they must agree.
            if (capturedEvent.Tags.Count > 0)
            {
                AssertThat.IsTrue(
                    capturedEvent.Tags.Contains(capturedEvent.Source!),
                    $"AsyncLocal Source '{capturedEvent.Source}' must match the TagWith tag " +
                    $"baked into the same command ({string.Join(",", capturedEvent.Tags)})");
            }
        }

        // sanity — both kinds of operation actually ran under load
        AssertThat.IsTrue(
            events.Any(e => e.Source == "SqlPersonRepository.GetByIdAsync"),
            "concurrent reads should have been captured");
        AssertThat.IsTrue(
            events.Any(e => e.Source == "SqlPersonRepository.SaveAsync"),
            "concurrent writes should have been captured");
    }

    [Fact]
    public async Task WithoutDiagnostics_NoEventsCaptured()
    {
        await EnsureCleanDatabaseAsync();

        // Build a provider that registers a sink but never calls
        // WithQueryDiagnostics — so no interceptor is wired, zero overhead.
        var sink = new CapturingEfCoreQueryLogSink();
        var services = new ServiceCollection();
        services.AddBendayEfCore<TestDbContext>(options =>
        {
            options.UseConnectionString(ConnectionString);
            options.WithQueryLogSink(sink);
            options.RegisterDbContext();
            options.RegisterUsernameProvider<EnvironmentUsernameProvider>();
            options.RegisterAggregate<
                IPersonRepository, SqlPersonRepository,
                PersonAdapter,
                PersonDomainModel,
                IPersonService, PersonService>();
        });
        await using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();
            await repo.SaveAsync(new Person { FirstName = "Alan", LastName = "Turing" });
        }

        sink.Events.Count.ShouldEqual(0,
            "No interceptor is registered without WithQueryDiagnostics, so nothing is captured");
    }
}

/// <summary>
/// In-memory capturing sink for tests. Records every event synchronously
/// (no background flushing) so assertions can run immediately after the
/// awaited operation completes. This is the seed of the #3 assertion helpers.
/// </summary>
internal sealed class CapturingEfCoreQueryLogSink : IEfCoreQueryLogSink
{
    private readonly List<EfCoreQueryDiagnostics> _events = new();
    private readonly Lock _gate = new();

    public IReadOnlyList<EfCoreQueryDiagnostics> Events
    {
        get { lock (_gate) { return _events.ToList(); } }
    }

    public void Record(EfCoreQueryDiagnostics diagnostics)
    {
        lock (_gate) { _events.Add(diagnostics); }
    }

    public void Clear()
    {
        lock (_gate) { _events.Clear(); }
    }
}
