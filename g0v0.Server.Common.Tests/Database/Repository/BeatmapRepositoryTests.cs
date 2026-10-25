// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using BeatmapOnlineStatus = osu.Game.Beatmaps.BeatmapOnlineStatus;
using MySqlBeatmapRepository = g0v0.Server.Common.Database.MySQL.Repository.BeatmapRepository;
using MySqlDbContext = g0v0.Server.Common.Database.MySQL.MysqlDbContext;
using PostgreSqlBeatmapRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.BeatmapRepository;
using PostgreSqlDbContext = g0v0.Server.Common.Database.PostgreSQL.PostgreSqlDbContext;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="IBeatmapRepository"/> contract against both database backends.
/// </summary>
[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class BeatmapRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IBeatmapRepository _repository = null!;

    private DbSet<Beatmap> Beatmaps => _context.Set<Beatmap>();

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        _repository = backend == RepositoryBackend.MySql
            ? new MySqlBeatmapRepository((MySqlDbContext)_context)
            : new PostgreSqlBeatmapRepository((PostgreSqlDbContext)_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByChecksumAsync_WhenBeatmapExists_ShouldReturnBeatmap()
    {
        Beatmaps.Add(CreateBeatmap(id: 1, checksum: "abc-checksum"));
        await _context.SaveChangesAsync();

        Beatmap? result = await _repository.GetByChecksumAsync("abc-checksum");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByMapperIdAsync_WhenBeatmapsExist_ShouldReturnMatchingBeatmaps()
    {
        Beatmaps.AddRange(
            CreateBeatmap(id: 1, mapperId: 100),
            CreateBeatmap(id: 2, mapperId: 100),
            CreateBeatmap(id: 3, mapperId: 200));
        await _context.SaveChangesAsync();

        IReadOnlyList<Beatmap> results = await _repository.GetByMapperIdAsync(100);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<Beatmap>(beatmap => beatmap.MapperId == 100));
    }

    [Test]
    public async Task GetOrFetchByIdAsync_WhenBeatmapExists_ShouldNotFetch()
    {
        Beatmaps.Add(CreateBeatmap(id: 321));
        await _context.SaveChangesAsync();

        FakeFetcher fetcher = new();
        Beatmap result = await _repository.GetOrFetchByIdAsync(321, fetcher);

        Assert.That(result.Id, Is.EqualTo(321));
        Assert.That(fetcher.BeatmapFetchCount, Is.Zero);
    }

    [Test]
    public async Task GetOrFetchByIdAsync_WhenBeatmapMissing_ShouldFetchAndReturn()
    {
        FakeFetcher fetcher = new(beatmapFactory: id => CreateBeatmap(id: id));
        Beatmap result = await _repository.GetOrFetchByIdAsync(321, fetcher);

        Assert.That(result.Id, Is.EqualTo(321));
        Assert.That(fetcher.BeatmapFetchCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrFetchByIdAsync_WhenFetcherThrows_ShouldPropagate()
    {
        FakeFetcher fetcher = new(exception: new InvalidOperationException("boom"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _repository.GetOrFetchByIdAsync(321, fetcher));
        Assert.That(Beatmaps.Any(b => b.Id == 321), Is.False);
    }

    [Test]
    public async Task CreateUpdateDeleteAsync_ShouldPersistBeatmapChanges()
    {
        Beatmap beatmap = CreateBeatmap(id: 10, version: "Hard");

        await _repository.CreateAsync(beatmap);
        beatmap.Version = "Extra";
        await _repository.UpdateAsync(beatmap);
        await _repository.DeleteAsync(beatmap);

        bool exists = await Beatmaps.AnyAsync(b => b.Id == 10);
        Assert.That(exists, Is.False);
    }

    private static Beatmap CreateBeatmap(
        int id,
        int beatmapSetId = 456,
        string? checksum = "checksum_1",
        string version = "Insane",
        int mapperId = 789) => new()
        {
            Id = id,
            BeatmapSetId = beatmapSetId,
            Url = $"https://osu.example/beatmaps/{id}",
            Mode = 3,
            DifficultyRating = 5.2,
            TotalLength = 120,
            MapperId = mapperId,
            Version = version,
            Checksum = checksum,
            MaxCombo = 1000,
            ApproachRate = 9.5f,
            CircleSize = 4f,
            HpDrainRate = 6.5f,
            OverallDifficulty = 8.7f,
            Bpm = 180f,
            CirclesCount = 300,
            SlidersCount = 200,
            SpinnersCount = 2,
            DeletedAt = null,
            HitLength = 100,
            LastUpdate = DateTimeOffset.UtcNow,
            Status = BeatmapOnlineStatus.Ranked,
        };
}