// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Beatmaps;
using Beatmap = g0v0.Server.Common.Database.Models.Beatmap;
using BeatmapSet = g0v0.Server.Common.Database.Models.BeatmapSet;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlBeatmapSetRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private BeatmapSetRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PostgreSqlDbContext> options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new BeatmapSetRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenSetExists_ShouldReturnSet()
    {
        _context.BeatmapSets.Add(CreateSet(id: 55));
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
        _context.BeatmapSets.Add(set);
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
        Assert.That(_context.BeatmapSets.Count(), Is.EqualTo(1));
        Assert.That(_context.Beatmaps.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpsertWithBeatmapsAsync_WhenSetExists_ShouldUpdateValues()
    {
        BeatmapSet existing = CreateSet(id: 55);
        _context.BeatmapSets.Add(existing);
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
        _context.BeatmapSets.Add(existing);
        await _context.SaveChangesAsync();

        BeatmapSet incoming = CreateSet(id: 55);
        incoming.Beatmaps.Add(CreateBeatmap(id: 2, 55));

        await _repository.UpsertWithBeatmapsAsync(incoming);

        Assert.That(_context.Beatmaps.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveSet()
    {
        BeatmapSet set = CreateSet(id: 55);
        _context.BeatmapSets.Add(set);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(set);

        Assert.That(_context.BeatmapSets.Any(s => s.Id == 55), Is.False);
    }

    [Test]
    public void BeatmapSetRepository_ShouldImplementIPostgreSqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IPostgreSqlRepository>());
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