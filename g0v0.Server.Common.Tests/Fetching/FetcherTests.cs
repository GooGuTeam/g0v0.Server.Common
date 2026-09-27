// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Fetching;
using g0v0.Server.Common.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using BeatmapModel = g0v0.Server.Common.Database.Models.Beatmap;
using BeatmapSetModel = g0v0.Server.Common.Database.Models.BeatmapSet;

namespace g0v0.Server.Common.Tests.Fetching;

[TestFixture]
public class FetcherTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_tempDir, "config"));
        WriteGeneralConfig(new GeneralConfig
        {
            FetcherClientId = 1,
            FetcherClientSecret = "secret",
            FetcherBeatmapRawCacheExpireHours = 24,
        });
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public async Task FetchBeatmapSetAsync_ShouldPersistSetAndBeatmaps()
    {
        StubHttpMessageHandler handler = new(
            ("/oauth/token", TokenResponse("access-1", 3600)),
            ("/api/v2/beatmapsets/55", SetResponse()));
        FakeStringCache cache = new();
        FakeBeatmapSetRepository repository = new();
        Fetcher fetcher = CreateFetcher(handler, cache, repository);

        BeatmapSetModel set = await fetcher.FetchBeatmapSetAsync(55);

        Assert.That(set.Id, Is.EqualTo(55));
        Assert.That(set.Beatmaps, Has.Count.EqualTo(2));
        Assert.That(repository.Sets, Contains.Key(55));
    }

    [Test]
    public async Task FetchBeatmapAsync_ByBeatmapId_ShouldLookupThenFetchSet()
    {
        StubHttpMessageHandler handler = new(
            ("/oauth/token", TokenResponse("access-1", 3600)),
            ("/api/v2/beatmaps/lookup?id=5802334", LookupResponse(5802334)),
            ("/api/v2/beatmapsets/55", SetResponse()));
        FakeStringCache cache = new();
        FakeBeatmapSetRepository repository = new();
        Fetcher fetcher = CreateFetcher(handler, cache, repository);

        BeatmapModel beatmap = await fetcher.FetchBeatmapAsync(5802334);

        Assert.That(beatmap.Id, Is.EqualTo(5802334));
        Assert.That(beatmap.BeatmapSetId, Is.EqualTo(55));
        Assert.That(repository.Sets, Contains.Key(55));
    }

    [Test]
    public async Task FetchBeatmapSetAsync_ShouldCacheAccessToken()
    {
        StubHttpMessageHandler handler = new(
            ("/oauth/token", TokenResponse("access-1", 3600)),
            ("/api/v2/beatmapsets/55", SetResponse()));
        FakeStringCache cache = new();
        Fetcher fetcher = CreateFetcher(handler, cache, new FakeBeatmapSetRepository());

        await fetcher.FetchBeatmapSetAsync(55);

        Assert.That(await cache.GetAsync("fetcher:access_token:1"), Is.EqualTo("access-1"));
        Assert.That(await cache.GetAsync("fetcher:expire_at:1"), Is.Not.Null);
    }

    [Test]
    public async Task FetchBeatmapSetAsync_NewInstanceWithCachedToken_ShouldNotRequestToken()
    {
        FakeStringCache cache = new();
        cache.Seed("fetcher:access_token:1", "cached-token");
        cache.Seed("fetcher:expire_at:1", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());

        StubHttpMessageHandler handler = new(
            ("/api/v2/beatmapsets/55", SetResponse()));
        Fetcher fetcher = CreateFetcher(handler, cache, new FakeBeatmapSetRepository());

        await fetcher.FetchBeatmapSetAsync(55);

        Assert.That(handler.RequestUris, Has.No.Member("https://osu.ppy.sh/oauth/token"));
    }

    [Test]
    public Task FetchBeatmapSetAsync_Unauthorized_ShouldRefreshTokenAndRetryOnce()
    {
        StubHttpMessageHandler handler = new(
            ("/oauth/token", TokenResponse("access-2", 3600)),
            ("/api/v2/beatmapsets/55", UnauthorizedResponse()));
        FakeStringCache cache = new();
        cache.Seed("fetcher:access_token:1", "expired-token");
        cache.Seed("fetcher:expire_at:1", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());
        Fetcher fetcher = CreateFetcher(handler, cache, new FakeBeatmapSetRepository());

        Assert.ThrowsAsync<HttpServiceException>(() => fetcher.FetchBeatmapSetAsync(55));
        return Task.CompletedTask;
    }

    [Test]
    public async Task FakeBeatmapSetRepository_GetOrFetchByIdAsync_WhenStored_ShouldNotFetch()
    {
        FakeBeatmapSetRepository repository = new();
        BeatmapSetModel stored = new() { Id = 55, Artist = "stored", Title = "Stored Set", Creator = "mapper", CreatorId = 1, SubmittedDate = DateTimeOffset.UtcNow };
        await repository.UpsertWithBeatmapsAsync(stored);

        int fetchCount = 0;
        TrackingFetcher fetcher = new(() => fetchCount++);

        BeatmapSetModel result = await repository.GetOrFetchByIdAsync(55, fetcher);

        Assert.That(result.Title, Is.EqualTo("Stored Set"));
        Assert.That(fetchCount, Is.Zero);
    }

    [Test]
    public async Task FetchBeatmapRawAsync_WhenCached_ShouldNotHitNetwork()
    {
        StubHttpMessageHandler handler = new(("unused", new StubResponse(System.Net.HttpStatusCode.OK, "unused")));
        FakeStringCache cache = new();
        cache.Seed("beatmap:5802334:raw", "osu file format v14");
        Fetcher fetcher = CreateFetcher(handler, cache, new FakeBeatmapSetRepository());

        string raw = await fetcher.FetchBeatmapRawAsync(5802334);

        Assert.That(raw, Is.EqualTo("osu file format v14"));
        Assert.That(handler.RequestCount, Is.Zero);
    }

    [Test]
    public async Task FetchBeatmapRawAsync_OfficialFails_MirrorSucceeds()
    {
        StubHttpMessageHandler handler = new(
            ("/osu/5802334", new StubResponse(System.Net.HttpStatusCode.NotFound, string.Empty)),
            ("/api/osu/5802334", new StubResponse(System.Net.HttpStatusCode.OK, "osu file format v14", "text/plain")));
        FakeStringCache cache = new();
        Fetcher fetcher = CreateFetcher(handler, cache, new FakeBeatmapSetRepository());

        string raw = await fetcher.FetchBeatmapRawAsync(5802334);

        Assert.That(raw, Is.EqualTo("osu file format v14"));
        Assert.That(await cache.GetAsync("beatmap:5802334:raw"), Is.EqualTo("osu file format v14"));
    }

    [Test]
    public async Task FetchBeatmapRawAsync_AllSourcesFail_ShouldThrow()
    {
        StubHttpMessageHandler handler = new(
            ("/osu/5802334", new StubResponse(System.Net.HttpStatusCode.NotFound, string.Empty)),
            ("/api/osu/5802334", new StubResponse(System.Net.HttpStatusCode.NotFound, string.Empty)),
            ("/osu/5802334", new StubResponse(System.Net.HttpStatusCode.NotFound, string.Empty)));
        FakeStringCache cache = new();
        Fetcher fetcher = CreateFetcher(handler, cache, new FakeBeatmapSetRepository());

        Assert.ThrowsAsync<FetcherException>(() => fetcher.FetchBeatmapRawAsync(5802334));
        Assert.That(await cache.GetAsync("beatmap:5802334:raw"), Is.Null);
    }

    [Test]
    public void FetchBeatmapAsync_InvalidId_ShouldThrow()
    {
        Fetcher fetcher = CreateFetcher(new StubHttpMessageHandler((_, _) => new StubResponse(System.Net.HttpStatusCode.OK, "{}")), new FakeStringCache(), new FakeBeatmapSetRepository());

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fetcher.FetchBeatmapAsync(0));
    }

    private static StubResponse TokenResponse(string token, int expiresIn)
    {
        return new StubResponse(System.Net.HttpStatusCode.OK, JsonConvert.SerializeObject(new { access_token = token, expires_in = expiresIn }));
    }

    private static StubResponse UnauthorizedResponse()
    {
        return new StubResponse(System.Net.HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}");
    }

    private static StubResponse SetResponse()
    {
        return new StubResponse(System.Net.HttpStatusCode.OK, JsonConvert.SerializeObject(new
        {
            id = 55,
            status = "ranked",
            artist = "Artist",
            artist_unicode = "アーティスト",
            title = "Title",
            title_unicode = "タイトル",
            creator = "mapper",
            user_id = 100,
            preview_url = "https://example.com/preview.mp3",
            source = string.Empty,
            tags = "tag1 tag2",
            bpm = 180.0,
            nsfw = false,
            spotlight = false,
            video = false,
            storyboard = false,
            submitted_date = "2024-01-01T00:00:00Z",
            beatmaps = new[]
            {
                new
                {
                    id = 5802334,
                    beatmapset_id = 55,
                    status = "ranked",
                    mode_int = 0,
                    checksum = "abc123",
                    url = "https://osu.ppy.sh/beatmaps/5802334",
                    version = "Insane",
                    user_id = 100,
                    difficulty_rating = 5.2,
                    total_length = 100,
                    hit_length = 90,
                    ar = 9.0f,
                    cs = 4.0f,
                    drain = 6.0f,
                    accuracy = 8.5f,
                    bpm = 180.0f,
                    count_circles = 100,
                    count_sliders = 50,
                    count_spinners = 2,
                    max_combo = 500,
                },
                new
                {
                    id = 5802335,
                    beatmapset_id = 55,
                    status = "ranked",
                    mode_int = 0,
                    checksum = "def456",
                    url = "https://osu.ppy.sh/beatmaps/5802335",
                    version = "Hard",
                    user_id = 100,
                    difficulty_rating = 4.2,
                    total_length = 110,
                    hit_length = 95,
                    ar = 8.0f,
                    cs = 4.0f,
                    drain = 5.0f,
                    accuracy = 7.5f,
                    bpm = 180.0f,
                    count_circles = 80,
                    count_sliders = 40,
                    count_spinners = 1,
                    max_combo = 400,
                },
            },
        }));
    }

    private static StubResponse LookupResponse(int beatmapId)
    {
        return new StubResponse(System.Net.HttpStatusCode.OK, JsonConvert.SerializeObject(new
        {
            id = beatmapId,
            beatmapset_id = 55,
            status = "ranked",
            mode_int = 0,
            checksum = "abc123",
            url = $"https://osu.ppy.sh/beatmaps/{beatmapId}",
            version = "Insane",
            user_id = 100,
            difficulty_rating = 5.2,
            total_length = 100,
            hit_length = 90,
            ar = 9.0f,
            cs = 4.0f,
            drain = 6.0f,
            accuracy = 8.5f,
            bpm = 180.0f,
            count_circles = 100,
            count_sliders = 50,
            count_spinners = 2,
            max_combo = 500,
        }));
    }

    private Fetcher CreateFetcher(StubHttpMessageHandler handler, FakeStringCache cache, FakeBeatmapSetRepository repository)
    {
        HttpClient client = new(handler);
        HttpService httpService = new(client);
        ConfigManager manager = new(_tempDir);
        return new Fetcher(httpService, manager, cache, repository, NullLogger<Fetcher>.Instance);
    }

    private void WriteGeneralConfig(GeneralConfig config)
    {
        string json = JsonConvert.SerializeObject(config);
        File.WriteAllText(Path.Combine(_tempDir, "config", "general.json"), json);
    }

    private sealed class TrackingFetcher : IFetcher
    {
        private readonly Action _onFetch;

        public TrackingFetcher(Action onFetch)
        {
            _onFetch = onFetch;
        }

        public Task<BeatmapModel> FetchBeatmapAsync(int beatmapId, CancellationToken cancellationToken = default)
        {
            _onFetch();
            return Task.FromResult(new BeatmapModel { Id = beatmapId, BeatmapSetId = beatmapId, Version = "fetched", Mode = 0, MapperId = 1, TotalLength = 1 });
        }

        public Task<BeatmapModel> FetchBeatmapAsync(string checksum, CancellationToken cancellationToken = default)
        {
            _onFetch();
            return Task.FromResult(new BeatmapModel { Id = 0, BeatmapSetId = 0, Checksum = checksum, Version = "fetched", Mode = 0, MapperId = 1, TotalLength = 1 });
        }

        public Task<BeatmapSetModel> FetchBeatmapSetAsync(int beatmapSetId, CancellationToken cancellationToken = default)
        {
            _onFetch();
            return Task.FromResult(new BeatmapSetModel { Id = beatmapSetId, Artist = "fetched", Title = "Fetched Set", Creator = "mapper", CreatorId = 1, SubmittedDate = DateTimeOffset.UtcNow });
        }

        public Task<string> FetchBeatmapRawAsync(int beatmapId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}