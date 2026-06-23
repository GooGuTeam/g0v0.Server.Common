// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.MySQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Beatmaps;
using BeatmapModel = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class MySqlBeatmapRepositoryTests
{
    private MysqlDbContext _context = null!;
    private BeatmapRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MysqlDbContext(options);
        _repository = new BeatmapRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenBeatmapExists_ShouldReturnBeatmap()
    {
        var beatmap = CreateBeatmap(id: 321);
        _context.Beatmaps.Add(beatmap);
        await _context.SaveChangesAsync();

        BeatmapModel? result = await _repository.GetByIdAsync(321);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(321));
        Assert.That(result.Version, Is.EqualTo("Insane"));
    }

    [Test]
    public async Task GetByChecksumAsync_WhenBeatmapExists_ShouldReturnBeatmap()
    {
        var beatmap = CreateBeatmap(checksum: "abc123");
        _context.Beatmaps.Add(beatmap);
        await _context.SaveChangesAsync();

        BeatmapModel? result = await _repository.GetByChecksumAsync("abc123");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Checksum, Is.EqualTo("abc123"));
    }

    [Test]
    public async Task GetByBeatmapSetIdAsync_WhenBeatmapsExist_ShouldReturnMatchingBeatmaps()
    {
        _context.Beatmaps.AddRange(
            CreateBeatmap(id: 1, beatmapSetId: 55),
            CreateBeatmap(id: 2, beatmapSetId: 55),
            CreateBeatmap(id: 3, beatmapSetId: 99));
        await _context.SaveChangesAsync();

        IReadOnlyList<BeatmapModel> results = await _repository.GetByBeatmapSetIdAsync(55);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<BeatmapModel>(b => b.BeatmapSetId == 55));
    }

    [Test]
    public async Task GetByMapperIdAsync_WhenBeatmapsExist_ShouldReturnMatchingBeatmaps()
    {
        _context.Beatmaps.AddRange(
            CreateBeatmap(id: 1, mapperId: 10),
            CreateBeatmap(id: 2, mapperId: 10),
            CreateBeatmap(id: 3, mapperId: 20));
        await _context.SaveChangesAsync();

        IReadOnlyList<BeatmapModel> results = await _repository.GetByMapperIdAsync(10);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<BeatmapModel>(b => b.MapperId == 10));
    }

    [Test]
    public async Task CreateAsync_ShouldPersistBeatmap()
    {
        var beatmap = CreateBeatmap();

        await _repository.CreateAsync(beatmap);

        bool exists = await _context.Beatmaps.AnyAsync(b => b.Checksum == beatmap.Checksum);
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var beatmap = CreateBeatmap(version: "Hard");
        _context.Beatmaps.Add(beatmap);
        await _context.SaveChangesAsync();

        beatmap.Version = "Extra";
        beatmap.Status = BeatmapOnlineStatus.Loved;
        await _repository.UpdateAsync(beatmap);

        BeatmapModel? updated = await _context.Beatmaps.FirstOrDefaultAsync(b => b.Id == beatmap.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Version, Is.EqualTo("Extra"));
        Assert.That(updated.Status, Is.EqualTo(BeatmapOnlineStatus.Loved));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveBeatmap()
    {
        var beatmap = CreateBeatmap(id: 404);
        _context.Beatmaps.Add(beatmap);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(beatmap);

        bool exists = await _context.Beatmaps.AnyAsync(b => b.Id == 404);
        Assert.That(exists, Is.False);
    }

    [Test]
    public void BeatmapRepository_ShouldImplementIMySqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IMySqlRepository>());
    }

    [Test]
    public void MysqlDbContext_ShouldApplyBeatmapConfigurationAutomatically()
    {
        var options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseMySql(
                "Server=localhost;Database=g0v0_test;User=root;Password=test;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        using var relationalContext = new MysqlDbContext(options);

        var entityType = relationalContext.Model.FindEntityType(typeof(BeatmapModel));

        Assert.That(entityType, Is.Not.Null);
        Assert.That(entityType!.GetTableName(), Is.EqualTo("beatmaps"));

        Assert.Multiple(() =>
        {
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.Url))!.GetColumnName(), Is.EqualTo("url"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.Mode))!.GetColumnName(), Is.EqualTo("mode"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.Mode))!.GetColumnType(), Is.EqualTo("enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.Mode))!.GetValueConverter(), Is.Not.Null);
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.Status))!.GetColumnName(), Is.EqualTo("beatmap_status"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.Status))!.GetColumnType(), Is.EqualTo("enum('GRAVEYARD','WIP','PENDING','RANKED','APPROVED','QUALIFIED','LOVED')"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.Status))!.GetValueConverter(), Is.Not.Null);
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.MapperId))!.GetColumnName(), Is.EqualTo("user_id"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.DifficultyRating))!.GetColumnName(), Is.EqualTo("difficulty_rating"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.LastUpdate))!.GetColumnName(), Is.EqualTo("last_updated"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.LastUpdate))!.GetColumnType(), Is.EqualTo("datetime"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.DeletedAt))!.GetColumnName(), Is.EqualTo("deleted_at"));
            Assert.That(entityType.FindProperty(nameof(BeatmapModel.BeatmapSetId))!.GetColumnName(), Is.EqualTo("beatmapset_id"));
        });

        AssertIndexName(entityType, nameof(BeatmapModel.Status), "ix_beatmaps_beatmap_status");
        AssertIndexName(entityType, nameof(BeatmapModel.BeatmapSetId), "ix_beatmaps_beatmapset_id");
        AssertIndexName(entityType, nameof(BeatmapModel.Checksum), "ix_beatmaps_checksum");
        AssertIndexName(entityType, nameof(BeatmapModel.DifficultyRating), "ix_beatmaps_difficulty_rating");
        AssertIndexName(entityType, nameof(BeatmapModel.Id), "ix_beatmaps_id");
        AssertIndexName(entityType, nameof(BeatmapModel.LastUpdate), "ix_beatmaps_last_updated");
        AssertIndexName(entityType, nameof(BeatmapModel.MapperId), "ix_beatmaps_user_id");
        AssertIndexName(entityType, nameof(BeatmapModel.Version), "ix_beatmaps_version");
    }

    private static void AssertIndexName(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, string propertyName, string expectedName)
    {
        var index = entityType.GetIndexes().Single(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
        Assert.That(index.GetDatabaseName(), Is.EqualTo(expectedName));
    }

    private static BeatmapModel CreateBeatmap(
        int id = 123,
        long beatmapSetId = 456,
        string? checksum = "checksum_1",
        string version = "Insane",
        int mapperId = 789)
    {
        return new BeatmapModel
        {
            Id = id,
            BeatmapSetId = beatmapSetId,
            Url = "https://osu.example/beatmaps/123",
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
}