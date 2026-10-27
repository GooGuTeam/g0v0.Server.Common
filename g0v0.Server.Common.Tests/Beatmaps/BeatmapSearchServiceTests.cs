// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using g0v0.Server.Common.Beatmaps;
using g0v0.Server.Common.Caching;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Extensions;
using g0v0.Server.Common.Fetching;
using g0v0.Server.Common.Http;
using g0v0.Server.Common.Tests.Database.Repository;
using g0v0.Server.Common.Tests.Fetching;
using g0v0.Server.Common.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Beatmaps;

public class BeatmapSearchServiceTests
{
    private string _directory = null!;
    private ServiceProvider _services = null!;
    private BeatmapSearchService _search = null!;
    private RecordingSource _source = null!;
    private RecordingCache _cache = null!;
    private QueuedTasks _tasks = null!;
    private FakeBeatmapRepository _beatmaps = null!;
    private FakeBeatmapSetRepository _sets = null!;
    private FakeFetcher _fetcher = null!;
    private CancellationTokenSource _stopping = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "beatmap-search-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(_directory, "config"));
        BeatmapSearchConfig options = new()
        {
            EnableWarmup = false,
            PrefetchDelay = TimeSpan.Zero,
            PrefetchPageDelay = TimeSpan.Zero,
            WarmupQueryDelay = TimeSpan.Zero
        };
        File.WriteAllText(Path.Combine(_directory, "config", "beatmap_search.json"), JsonConvert.SerializeObject(options));
        _stopping = new CancellationTokenSource();
        _source = new RecordingSource();
        _cache = new RecordingCache();
        _tasks = new QueuedTasks();
        _beatmaps = new FakeBeatmapRepository();
        _sets = new FakeBeatmapSetRepository();
        _fetcher = new FakeFetcher(beatmapFactory: id => new Beatmap { Id = id, BeatmapSetId = 55 });
        _services = new ServiceCollection().AddLogging()
            .AddSingleton(new ConfigManager(_directory))
            .AddSingleton<IHostApplicationLifetime>(new TestLifetime(_stopping))
            .AddSingleton<IStringCache>(_cache)
            .AddSingleton<IBackgroundTaskRunner>(_tasks)
            .AddScoped<IBeatmapSearchSource>(_ => _source)
            .AddScoped<IBeatmapRepository>(_ => _beatmaps)
            .AddScoped<IBeatmapSetRepository>(_ => _sets)
            .AddScoped<IFetcher>(_ => _fetcher)
            .AddBeatmapSearch()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _search = _services.GetRequiredService<BeatmapSearchService>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _stopping.CancelAsync();
        await _services.DisposeAsync();
        _stopping.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public async Task SearchCacheNormalizesFiltersButSeparatesEveryCursorAndSearchOption()
    {
        BeatmapSearchQuery query = new() { Query = " song ", General = "converts.featured_artists" };
        await _search.SearchAsync(query);
        await _search.SearchAsync(query with { Query = "song", General = "featured_artists.converts.converts" });
        Assert.That(_source.Calls, Has.Count.EqualTo(1));

        await _search.SearchAsync(query, "cursor-A");
        await _search.SearchAsync(query, "cursor-B");
        await _search.SearchAsync(query with { Mode = 3 });
        await _search.SearchAsync(query with { Status = "any" });
        await _search.SearchAsync(query with { Sort = "plays_desc" });
        await _search.SearchAsync(query with { Language = "japanese" });
        await _search.SearchAsync(query with { Extras = "video" });
        await _search.SearchAsync(query with { IncludeExplicitContent = true });
        Assert.That(_source.Calls, Has.Count.EqualTo(9));
        Assert.That(_cache.Values.Values.Select(value => value.Lifetime), Is.All.EqualTo(TimeSpan.FromMinutes(15)));
    }

    [TestCase("not json")]
    [TestCase("{}")]
    public async Task CorruptAndExpiredPagesAreFetchedAgain(string corrupted)
    {
        BeatmapSearchQuery query = new();
        await _search.SearchAsync(query);
        string key = _cache.Values.Keys.Single();
        _cache.Values[key] = (corrupted, TimeSpan.Zero);
        await _search.SearchAsync(query);
        _cache.Values.Clear();
        await _search.SearchAsync(query);
        Assert.That(_source.Calls, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task ConcurrentMissesShareOneRequestAndCancellingAWaiterDoesNotCancelOthers()
    {
        TaskCompletionSource<BeatmapSearchResult> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _source.Respond = (_, _, _) => response.Task;
        using CancellationTokenSource cancelled = new();
        Task<BeatmapSearchResult> first = _search.SearchAsync(new BeatmapSearchQuery(), cancellationToken: cancelled.Token);
        Task<BeatmapSearchResult> second = _search.SearchAsync(new BeatmapSearchQuery());
        Assert.That(_source.Calls, Has.Count.EqualTo(1));
        await cancelled.CancelAsync();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await first);
        response.SetResult(Page(42));
        Assert.That((await second.WaitAsync(TimeSpan.FromSeconds(2))).BeatmapSets.Single().Id, Is.EqualTo(42));
        Assert.That((await _search.SearchAsync(new BeatmapSearchQuery())).BeatmapSets.Single().Id, Is.EqualTo(42));
        Assert.That(_source.Calls, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task HostShutdownCancelsTheSharedUpstreamRequest()
    {
        _source.Respond = async (_, _, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Page(1);
        };
        Task<BeatmapSearchResult> request = _search.SearchAsync(new BeatmapSearchQuery());
        await _stopping.CancelAsync();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await request.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public async Task CachedPagesPreserveSetMetadataAndDifficultyDetails()
    {
        BeatmapSet set = new()
        {
            Id = 42,
            Artist = "作曲者",
            Title = "Song",
            HasVideo = true,
            LastUpdatedDate = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero),
            Beatmaps = [new Beatmap { Id = 99, BeatmapSetId = 42, Version = "Insane", Mode = 3, DifficultyRating = 5.25, CircleSize = 4 }]
        };
        _source.Respond = (_, _, _) => Task.FromResult(new BeatmapSearchResult { Total = 1, BeatmapSets = [set] });
        await _search.SearchAsync(new BeatmapSearchQuery());
        BeatmapSet cached = (await _search.SearchAsync(new BeatmapSearchQuery())).BeatmapSets.Single();
        Assert.That(cached, Is.Not.SameAs(set));
        Assert.That(cached.Artist, Is.EqualTo(set.Artist));
        Assert.That(cached.LastUpdatedDate, Is.EqualTo(set.LastUpdatedDate));
        Assert.That(cached.HasVideo, Is.True);
        Assert.That(cached.Beatmaps.Single().DifficultyRating, Is.EqualTo(5.25));
        Assert.That(cached.Beatmaps.Single().Mode, Is.EqualTo(3));
        Assert.That(_source.Calls, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FailedRequestsAreNotRetainedAndCacheOutagesDoNotHideResults()
    {
        _source.Respond = (_, _, _) => throw new HttpServiceException("upstream failed", 502);
        Assert.ThrowsAsync<HttpServiceException>(() => _search.SearchAsync(new BeatmapSearchQuery()));
        _source.Respond = (_, _, _) => Task.FromResult(Page(55));
        _cache.Unavailable = true;
        Assert.That((await _search.SearchAsync(new BeatmapSearchQuery())).BeatmapSets.Single().Id, Is.EqualTo(55));
        Assert.That(_source.Calls, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SearchesPrefetchOnlyOnePageAndCacheTheTerminalPage()
    {
        _source.Respond = (_, cursor, _) => Task.FromResult(cursor is null ? Page(1, "next") : Page(2));
        BeatmapSearchQuery query = new() { Query = "song" };
        await _search.SearchAsync(query);
        await _search.SearchAsync(query);
        Assert.That(_source.Calls, Has.Count.EqualTo(1));
        Assert.That(_tasks.Pending, Has.Count.EqualTo(1));

        await _tasks.DrainAsync();
        BeatmapSearchResult last = await _search.SearchAsync(query, "next");
        Assert.That(last.BeatmapSets.Single().Id, Is.EqualTo(2));
        Assert.That(last.HasMore, Is.False);
        Assert.That(_source.Calls, Has.Count.EqualTo(2));
        Assert.That(_cache.Values.Values.Select(value => value.Lifetime), Is.EquivalentTo((TimeSpan[])[TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10)]));
        Assert.That(_tasks.Pending, Is.Empty);
    }

    [Test]
    public async Task AHomepageDoesNotPrefetchAndNavigationBeforeTheDelayAvoidsDuplicateRequests()
    {
        _source.Respond = (_, cursor, _) => Task.FromResult(cursor is null ? Page(1, "next") : Page(2));
        await _search.SearchAsync(new BeatmapSearchQuery());
        Assert.That(_tasks.Pending, Is.Empty);

        BeatmapSearchQuery query = new() { Status = "pending" };
        await _search.SearchAsync(query);
        await _search.SearchAsync(query, "next");
        await _tasks.DrainAsync();
        Assert.That(_source.Calls, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task WarmupLoadsFourHomepagesAndTwoPagesEachWithoutRecursiveScheduling()
    {
        _source.Respond = (_, cursor, _) => Task.FromResult(Page(1, cursor switch { null => "page2", "page2" => "page3", _ => "page4" }));
        await _search.WarmupAsync();
        await _search.WarmupAsync();
        Assert.That(_source.Calls, Has.Count.EqualTo(12));
        Assert.That(
            _source.Calls.Where(call => call.Cursor is null).Select(call => call.Query.Sort),
            Is.EquivalentTo((string[])["ranked_desc", "updated_desc", "favourites_desc", "plays_desc"]));
        Assert.That(_cache.Values.Values.Count(value => value.Lifetime == TimeSpan.FromMinutes(20)), Is.EqualTo(4));
        Assert.That(_tasks.Pending, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task WarmupContinuesAfterOneQueryFailsAndTheWorkerCanBeDisabled(bool timeout)
    {
        _source.Respond = (query, _, _) => string.Equals(query.Sort, "ranked_desc", StringComparison.Ordinal)
            ? throw (timeout ? new TaskCanceledException("upstream timeout") : new HttpServiceException("unavailable", 503))
            : Task.FromResult(Page(1));
        await _search.WarmupAsync();
        Assert.That(_source.Calls, Has.Count.EqualTo(4));
        IHostedService worker = _services.GetServices<IHostedService>().Single();
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        Assert.That(_source.Calls, Has.Count.EqualTo(4));
    }

    [TestCase("set")]
    [TestCase("beatmap")]
    [TestCase("checksum")]
    public async Task LookupResolvesStoredSetsByEachSupportedIdentifier(string kind)
    {
        BeatmapSet set = new() { Id = 55 };
        await _sets.UpsertWithBeatmapsAsync(set);
        await _beatmaps.CreateAsync(new Beatmap { Id = 42, BeatmapSetId = 55, Checksum = "abc" });
        BeatmapSet? found = kind switch
        {
            "set" => await _search.LookupAsync(beatmapSetId: 55),
            "beatmap" => await _search.LookupAsync(beatmapId: 42),
            _ => await _search.LookupAsync(checksum: "abc")
        };
        Assert.That(found, Is.SameAs(set));
        Assert.That(_fetcher.BeatmapFetchCount + _fetcher.SetFetchCount, Is.Zero);
    }

    [Test]
    public async Task LookupFetchesMissingDifficultyAndSetMetadata()
    {
        BeatmapSet? set = await _search.LookupAsync(beatmapId: 42);
        Assert.That(set!.Id, Is.EqualTo(55));
        Assert.That(_fetcher.BeatmapFetchCount, Is.EqualTo(1));
        Assert.That(_fetcher.SetFetchCount, Is.EqualTo(1));
    }

    private static BeatmapSearchResult Page(int id, string? cursor = null) => new()
    {
        BeatmapSets = [new BeatmapSet { Id = id }],
        Total = 10,
        Cursor = cursor
    };

    private sealed class TestLifetime(CancellationTokenSource stopping) : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => stopping.Token;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => stopping.Cancel();
    }

    private sealed class RecordingSource : IBeatmapSearchSource
    {
        public ConcurrentQueue<(BeatmapSearchQuery Query, string? Cursor)> Calls { get; } = new();

        public Func<BeatmapSearchQuery, string?, CancellationToken, Task<BeatmapSearchResult>> Respond { get; set; }
            = (_, _, _) => Task.FromResult(Page(1));

        public Task<BeatmapSearchResult> FetchSearchAsync(BeatmapSearchQuery query, string? cursor, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue((query, cursor));
            return Respond(query, cursor, cancellationToken);
        }
    }

    private sealed class RecordingCache : IStringCache
    {
        public ConcurrentDictionary<string, (string Value, TimeSpan Lifetime)> Values { get; } = new(StringComparer.Ordinal);

        public bool Unavailable { get; set; }

        public Task<string?> GetAsync(string key) => Unavailable
            ? throw new InvalidOperationException("cache unavailable")
            : Task.FromResult(Values.TryGetValue(key, out (string Value, TimeSpan Lifetime) value) ? value.Value : null);

        public Task SetAsync(string key, string value, TimeSpan expiry)
        {
            if (Unavailable)
            {
                throw new InvalidOperationException("cache unavailable");
            }

            Values[key] = (value, expiry);
            return Task.CompletedTask;
        }

        public Task<bool> TrySetAsync(string key, string value, TimeSpan expiry)
        {
            if (Unavailable)
            {
                throw new InvalidOperationException("cache unavailable");
            }

            if (Values.ContainsKey(key))
            {
                return Task.FromResult(false);
            }

            Values[key] = (value, expiry);
            return Task.FromResult(true);
        }

        public Task DeleteAsync(string key)
        {
            Values.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task RefreshAsync(string key, TimeSpan expiry) => Task.CompletedTask;
    }

    private sealed class QueuedTasks : IBackgroundTaskRunner
    {
        public Queue<Func<CancellationToken, Task>> Pending { get; } = new();

        public Task RunAsync(Action task, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task RunAsync(Func<Task> task, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task RunAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default)
        {
            Pending.Enqueue(task);
            return Task.CompletedTask;
        }

        public async Task DrainAsync()
        {
            while (Pending.TryDequeue(out Func<CancellationToken, Task>? task))
            {
                await task(CancellationToken.None);
            }
        }
    }
}