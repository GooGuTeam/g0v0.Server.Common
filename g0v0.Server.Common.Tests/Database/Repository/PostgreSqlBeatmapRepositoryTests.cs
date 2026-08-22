// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Beatmaps;
using BeatmapModel = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlBeatmapRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private BeatmapRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PostgreSqlDbContext> options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new BeatmapRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByChecksumAsync_WhenBeatmapExists_ShouldReturnBeatmap()
    {
        _context.Beatmaps.Add(CreateBeatmap(id: 1, checksum: "pg-checksum"));
        await _context.SaveChangesAsync();

        BeatmapModel? result = await _repository.GetByChecksumAsync("pg-checksum");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByMapperIdAsync_WhenBeatmapsExist_ShouldReturnMatchingBeatmaps()
    {
        _context.Beatmaps.AddRange(
            CreateBeatmap(id: 1, mapperId: 100),
            CreateBeatmap(id: 2, mapperId: 100),
            CreateBeatmap(id: 3, mapperId: 200));
        await _context.SaveChangesAsync();

        IReadOnlyList<BeatmapModel> results = await _repository.GetByMapperIdAsync(100);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<BeatmapModel>(beatmap => beatmap.MapperId == 100));
    }

    [Test]
    public async Task GetOrFetchByIdAsync_WhenBeatmapExists_ShouldNotFetch()
    {
        BeatmapModel beatmap = CreateBeatmap(id: 321);
        _context.Beatmaps.Add(beatmap);
        await _context.SaveChangesAsync();

        FakeFetcher fetcher = new();
        BeatmapModel result = await _repository.GetOrFetchByIdAsync(321, fetcher);

        Assert.That(result.Id, Is.EqualTo(321));
        Assert.That(fetcher.BeatmapFetchCount, Is.Zero);
    }

    [Test]
    public async Task GetOrFetchByIdAsync_WhenBeatmapMissing_ShouldFetchAndReturn()
    {
        FakeFetcher fetcher = new(beatmapFactory: id => CreateBeatmap(id: id));
        BeatmapModel result = await _repository.GetOrFetchByIdAsync(321, fetcher);

        Assert.That(result.Id, Is.EqualTo(321));
        Assert.That(fetcher.BeatmapFetchCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrFetchByIdAsync_WhenFetcherThrows_ShouldPropagate()
    {
        FakeFetcher fetcher = new(exception: new InvalidOperationException("boom"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _repository.GetOrFetchByIdAsync(321, fetcher));
        Assert.That(_context.Beatmaps.Any(b => b.Id == 321), Is.False);
    }

    [Test]
    public async Task CreateUpdateDeleteAsync_ShouldPersistBeatmapChanges()
    {
        BeatmapModel beatmap = CreateBeatmap(id: 10, version: "Hard");

        await _repository.CreateAsync(beatmap);
        beatmap.Version = "Extra";
        await _repository.UpdateAsync(beatmap);
        await _repository.DeleteAsync(beatmap);

        bool exists = await _context.Beatmaps.AnyAsync(b => b.Id == 10);
        Assert.That(exists, Is.False);
    }

    [Test]
    public void BeatmapRepository_ShouldImplementIPostgreSqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IPostgreSqlRepository>());
    }

    private static BeatmapModel CreateBeatmap(
        int id,
        int mapperId = 789,
        string? checksum = "checksum_1",
        string version = "Insane") => new()
        {
            Id = id,
            BeatmapSetId = 456,
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