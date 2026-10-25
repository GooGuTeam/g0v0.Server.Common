// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using BeatmapOnlineStatus = osu.Game.Beatmaps.BeatmapOnlineStatus;
using MySqlBeatmapSetRepository = g0v0.Server.Common.Database.MySQL.Repository.BeatmapSetRepository;
using MySqlDbContext = g0v0.Server.Common.Database.MySQL.MysqlDbContext;
using PostgreSqlBeatmapSetRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.BeatmapSetRepository;
using PostgreSqlDbContext = g0v0.Server.Common.Database.PostgreSQL.PostgreSqlDbContext;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="IBeatmapSetRepository"/> contract against both database backends.
/// </summary>
[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class BeatmapSetRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IBeatmapSetRepository _repository = null!;

    private DbSet<BeatmapSet> BeatmapSets => _context.Set<BeatmapSet>();

    private DbSet<Beatmap> Beatmaps => _context.Set<Beatmap>();

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        _repository = backend == RepositoryBackend.MySql
            ? new MySqlBeatmapSetRepository((MySqlDbContext)_context)
            : new PostgreSqlBeatmapSetRepository((PostgreSqlDbContext)_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenSetExists_ShouldReturnSet()
    {
        BeatmapSets.Add(CreateSet(id: 55));
        await _context.SaveChangesAsync();

        BeatmapSet? result = await _repository.GetByIdAsync(55);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Test Set"));
    }

    [Test]
    public async Task GetByIdWithBeatmapsAsync_WhenSetHasBeatmaps_ShouldIncludeBeatmaps()
    {
        BeatmapSet set = CreateSet(id: 55);
        set.Beatmaps.Add(CreateBeatmap(id: 1, set.Id));
        set.Beatmaps.Add(CreateBeatmap(id: 2, set.Id));
        BeatmapSets.Add(set);
        await _context.SaveChangesAsync();

        BeatmapSet? result = await _repository.GetByIdWithBeatmapsAsync(55);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Beatmaps, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpsertWithBeatmapsAsync_WhenSetIsNew_ShouldInsertSetAndBeatmaps()
    {
        BeatmapSet set = CreateSet(id: 55);
        set.Beatmaps.Add(CreateBeatmap(id: 1, set.Id));
        set.Beatmaps.Add(CreateBeatmap(id: 2, set.Id));

        BeatmapSet result = await _repository.UpsertWithBeatmapsAsync(set);

        Assert.That(result.Id, Is.EqualTo(55));
        Assert.That(BeatmapSets.Count(), Is.EqualTo(1));
        Assert.That(Beatmaps.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpsertWithBeatmapsAsync_WhenSetExists_ShouldUpdateValues()
    {
        BeatmapSet existing = CreateSet(id: 55);
        BeatmapSets.Add(existing);
        await _context.SaveChangesAsync();

        BeatmapSet updated = CreateSet(id: 55);
        updated.Artist = "New Artist";

        await _repository.UpsertWithBeatmapsAsync(updated);

        BeatmapSet? result = await _repository.GetByIdAsync(55);
        Assert.That(result!.Artist, Is.EqualTo("New Artist"));
    }

    [Test]
    public async Task UpsertWithBeatmapsAsync_WhenSetExists_ShouldNotDeleteMissingBeatmaps()
    {
        BeatmapSet existing = CreateSet(id: 55);
        existing.Beatmaps.Add(CreateBeatmap(id: 1, 55));
        BeatmapSets.Add(existing);
        await _context.SaveChangesAsync();

        BeatmapSet incoming = CreateSet(id: 55);
        incoming.Beatmaps.Add(CreateBeatmap(id: 2, 55));

        await _repository.UpsertWithBeatmapsAsync(incoming);

        Assert.That(Beatmaps.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetOrFetchByIdAsync_WhenSetExists_ShouldNotFetch()
    {
        BeatmapSets.Add(CreateSet(id: 55));
        await _context.SaveChangesAsync();

        FakeFetcher fetcher = new();
        BeatmapSet result = await _repository.GetOrFetchByIdAsync(55, fetcher);

        Assert.That(result.Id, Is.EqualTo(55));
        Assert.That(fetcher.SetFetchCount, Is.Zero);
    }

    [Test]
    public async Task GetOrFetchByIdAsync_WhenSetMissing_ShouldFetchAndReturn()
    {
        FakeFetcher fetcher = new(setFactory: id => CreateSet(id: id));
        BeatmapSet result = await _repository.GetOrFetchByIdAsync(55, fetcher);

        Assert.That(result.Id, Is.EqualTo(55));
        Assert.That(fetcher.SetFetchCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrFetchByIdAsync_WhenFetcherThrows_ShouldPropagate()
    {
        FakeFetcher fetcher = new(exception: new InvalidOperationException("boom"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _repository.GetOrFetchByIdAsync(55, fetcher));
        Assert.That(BeatmapSets.Any(s => s.Id == 55), Is.False);
    }

    [Test]
    public async Task GetOrFetchByIdAsync_ConcurrentMisses_ShouldFetchForEachMiss()
    {
        FakeFetcher fetcher = new(setFactory: id => CreateSet(id: id));

        Task<BeatmapSet>[] tasks = Enumerable.Range(0, 5)
            .Select(_ => _repository.GetOrFetchByIdAsync(55, fetcher))
            .ToArray();

        BeatmapSet[] results = await Task.WhenAll(tasks);

        Assert.That(results, Has.All.Matches<BeatmapSet>(s => s.Id == 55));
        Assert.That(fetcher.SetFetchCount, Is.EqualTo(5));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveSet()
    {
        BeatmapSet set = CreateSet(id: 55);
        BeatmapSets.Add(set);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(set);

        Assert.That(BeatmapSets.Any(s => s.Id == 55), Is.False);
    }

    private static BeatmapSet CreateSet(int id)
    {
        return new BeatmapSet
        {
            Id = id,
            Status = BeatmapOnlineStatus.Ranked,
            Artist = "Test Artist",
            ArtistUnicode = "テスト",
            Title = "Test Set",
            TitleUnicode = "テストセット",
            Creator = "mapper",
            CreatorId = 100,
            PreviewUrl = "https://example.com/preview.mp3",
            Source = string.Empty,
            SubmittedDate = DateTimeOffset.UtcNow,
        };
    }

    private static Beatmap CreateBeatmap(int id, int beatmapSetId)
    {
        return new Beatmap
        {
            Id = id,
            BeatmapSetId = beatmapSetId,
            Url = $"https://osu.ppy.sh/beatmaps/{id}",
            Version = "Insane",
            Mode = 0,
            TotalLength = 100,
            DifficultyRating = 5.2,
            MapperId = 100,
            Status = BeatmapOnlineStatus.Ranked,
        };
    }
}