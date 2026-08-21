// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.MySQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using ScoreModel = g0v0.Server.Common.Database.Models.Score;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class MySqlScoreRepositoryTests
{
    private MysqlDbContext _context = null!;
    private ScoreRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<MysqlDbContext> options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MysqlDbContext(options);
        _repository = new ScoreRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenScoreExists_ShouldReturnScore()
    {
        ScoreModel score = CreateScore(id: 111);
        _context.Scores.Add(score);
        await _context.SaveChangesAsync();

        ScoreModel? result = await _repository.GetByIdAsync(111);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(111));
            Assert.That(result.Rank, Is.EqualTo(ScoreRank.S));
            Assert.That(result.Statistics[HitResult.Great], Is.EqualTo(320));
        });
    }

    [Test]
    public async Task GetByBeatmapIdAsync_WhenScoresExist_ShouldReturnMatchingScores()
    {
        _context.Scores.AddRange(
            CreateScore(id: 1, beatmapId: 50),
            CreateScore(id: 2, beatmapId: 50),
            CreateScore(id: 3, beatmapId: 99));
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetByBeatmapIdAsync(50);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<ScoreModel>(s => s.BeatmapId == 50));
    }

    [Test]
    public async Task GetByUserIdAsync_WhenScoresExist_ShouldReturnMatchingScores()
    {
        _context.Scores.AddRange(
            CreateScore(id: 1, userId: 7),
            CreateScore(id: 2, userId: 7),
            CreateScore(id: 3, userId: 8));
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetByUserIdAsync(7);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<ScoreModel>(s => s.UserId == 7));
    }

    [Test]
    public async Task GetRecentByUserIdAndModeAsync_ShouldReturnOrderedLimitedScores()
    {
        _context.Scores.AddRange(
            CreateScore(id: 1, userId: 7, mode: 3, endedAt: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateScore(id: 2, userId: 7, mode: 3, endedAt: new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero)),
            CreateScore(id: 3, userId: 7, mode: 3, endedAt: new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero)),
            CreateScore(id: 4, userId: 7, mode: 0, endedAt: new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero)));
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetRecentByUserIdAndModeAsync(7, 3, 2);

        Assert.That(results.Select(s => s.Id), Is.EqualTo(new long[] { 2, 3 }));
    }

    [Test]
    public async Task GetBestByUserIdAndModeAsync_ShouldReturnOrderedLimitedScores()
    {
        _context.Scores.AddRange(
            CreateScore(id: 1, userId: 7, mode: 0, pp: 120),
            CreateScore(id: 2, userId: 7, mode: 0, pp: 350),
            CreateScore(id: 3, userId: 7, mode: 0, pp: 250),
            CreateScore(id: 4, userId: 7, mode: 3, pp: 999));
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetBestByUserIdAndModeAsync(7, 0, 3);

        Assert.That(results.Select(s => s.Id), Is.EqualTo(new long[] { 2, 3, 1 }));
    }

    [Test]
    public async Task GetScoreByToken_WhenTokenHasScore_ShouldReturnScore()
    {
        ScoreModel score = CreateScore(id: 500);
        _context.Scores.Add(score);
        _context.ScoreTokens.Add(new ScoreToken
        {
            Id = 600,
            ScoreId = score.Id,
            Score = score,
            UserId = score.UserId,
            BeatmapId = score.BeatmapId,
            RulesetId = score.Mode,
        });
        await _context.SaveChangesAsync();

        ScoreModel? result = await _repository.GetScoreByToken(600);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(500));
    }

    [Test]
    public async Task GetScoreByToken_WhenTokenHasNoScore_ShouldReturnNull()
    {
        _context.ScoreTokens.Add(new ScoreToken
        {
            Id = 601,
            UserId = 1,
            BeatmapId = 2,
            RulesetId = 0,
        });
        await _context.SaveChangesAsync();

        ScoreModel? result = await _repository.GetScoreByToken(601);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task MarkScoreHasReplay_WhenScoreExists_ShouldSetHasReplay()
    {
        _context.Scores.Add(CreateScore(id: 700, hasReplay: false));
        await _context.SaveChangesAsync();

        await _repository.MarkScoreHasReplay(700);

        bool hasReplay = await _context.Scores
            .Where(score => score.Id == 700)
            .Select(score => score.HasReplay)
            .SingleAsync();
        Assert.That(hasReplay, Is.True);
    }

    [Test]
    public async Task CreateAsync_ShouldPersistScore()
    {
        ScoreModel score = CreateScore(id: 1234, checksum: "persist-me");

        await _repository.CreateAsync(score);

        ScoreModel? persisted = await _context.Scores.SingleOrDefaultAsync(s => s.Id == 1234);
        Assert.That(persisted, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(persisted!.BeatmapChecksum, Is.EqualTo("persist-me"));
            Assert.That(persisted.Statistics[HitResult.Great], Is.EqualTo(320));
            Assert.That(persisted.MaximumStatistics[HitResult.Great], Is.EqualTo(400));
        });
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        ScoreModel score = CreateScore(id: 4321, pp: 200);
        _context.Scores.Add(score);
        await _context.SaveChangesAsync();

        score.Pp = 420;
        score.Rank = ScoreRank.XH;
        score.Statistics[HitResult.Miss] = 0;
        score.Statistics.Remove(HitResult.LargeTickMiss);
        score.MaximumStatistics[HitResult.Perfect] = 99;
        await _repository.UpdateAsync(score);

        ScoreModel? updated = await _context.Scores.SingleOrDefaultAsync(s => s.Id == 4321);
        Assert.That(updated, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updated!.Pp, Is.EqualTo(420));
            Assert.That(updated.Rank, Is.EqualTo(ScoreRank.XH));
            Assert.That(updated.Statistics[HitResult.Miss], Is.EqualTo(0));
            Assert.That(updated.Statistics.ContainsKey(HitResult.LargeTickMiss), Is.False);
            Assert.That(updated.MaximumStatistics[HitResult.Perfect], Is.EqualTo(99));
        });
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveScore()
    {
        ScoreModel score = CreateScore(id: 999);
        _context.Scores.Add(score);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(score);

        bool exists = await _context.Scores.AnyAsync(s => s.Id == 999);
        Assert.That(exists, Is.False);
    }

    [Test]
    public void ScoreRepository_ShouldImplementIMySqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IMySqlRepository>());
    }

    [Test]
    public void MysqlDbContext_ShouldApplyScoreConfigurationAutomatically()
    {
        DbContextOptions<MysqlDbContext> options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseMySql(
                "Server=localhost;Database=g0v0_test;User=root;Password=test;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        using MysqlDbContext relationalContext = new(options);

        Microsoft.EntityFrameworkCore.Metadata.IEntityType? entityType = relationalContext.Model.FindEntityType(typeof(ScoreModel));

        Assert.That(entityType, Is.Not.Null);
        Assert.That(entityType!.GetTableName(), Is.EqualTo("scores"));
        Assert.That(entityType.FindProperty(nameof(ScoreModel.Statistics)), Is.Null);
        Assert.That(entityType.FindProperty(nameof(ScoreModel.ClassicTotalScoreWithoutMods)), Is.Null);

        Assert.Multiple(() =>
        {
            Assert.That(entityType.FindProperty(nameof(ScoreModel.BeatmapChecksum))!.GetColumnName(), Is.EqualTo("map_md5"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Rank))!.GetColumnName(), Is.EqualTo("rank"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Rank))!.GetColumnType(), Is.EqualTo("enum('X','XH','S','SH','A','B','C','D','F')"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Rank))!.GetValueConverter(), Is.Not.Null);
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Mode))!.GetColumnName(), Is.EqualTo("gamemode"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Mode))!.GetColumnType(), Is.EqualTo("enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Mode))!.GetValueConverter(), Is.Not.Null);
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Mods))!.GetColumnType(), Is.EqualTo("json"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.Mods))!.GetValueConverter(), Is.Not.Null);
            Assert.That(entityType.FindProperty(nameof(ScoreModel.MaximumStatistics))!.GetColumnName(), Is.EqualTo("maximum_statistics"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.MaximumStatistics))!.GetColumnType(), Is.EqualTo("json"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.StartedAt))!.GetColumnType(), Is.EqualTo("datetime"));
            Assert.That(entityType.FindProperty(nameof(ScoreModel.EndedAt))!.GetColumnType(), Is.EqualTo("datetime"));
            Assert.That(entityType.FindProperty("N300")!.GetColumnName(), Is.EqualTo("n300"));
            Assert.That(entityType.FindProperty("NLargeTickMiss")!.GetColumnName(), Is.EqualTo("nlarge_tick_miss"));
        });

        AssertIndexName(entityType, [nameof(ScoreModel.UserId), nameof(ScoreModel.Mode), nameof(ScoreModel.EndedAt), nameof(ScoreModel.Id)], "idx_score_user_mode_date");
        AssertIndexName(entityType, [nameof(ScoreModel.UserId), nameof(ScoreModel.Mode), nameof(ScoreModel.Pp), nameof(ScoreModel.Id)], "idx_score_user_mode_pp");
        AssertIndexName(entityType, [nameof(ScoreModel.BeatmapId)], "ix_scores_beatmap_id");
        AssertIndexName(entityType, [nameof(ScoreModel.Mode)], "ix_scores_gamemode");
        AssertIndexName(entityType, [nameof(ScoreModel.BeatmapChecksum)], "ix_scores_map_md5");
        AssertIndexName(entityType, [nameof(ScoreModel.UserId)], "ix_scores_user_id");

        Microsoft.EntityFrameworkCore.Metadata.IForeignKey beatmapForeignKey = entityType.GetForeignKeys().Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual([nameof(ScoreModel.BeatmapId)], StringComparer.Ordinal));
        Assert.That(beatmapForeignKey.PrincipalEntityType.ClrType, Is.EqualTo(typeof(Beatmap)));
        Assert.That(beatmapForeignKey.GetConstraintName(), Is.EqualTo("scores_ibfk_1"));
        Assert.That(beatmapForeignKey.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict));

        Microsoft.EntityFrameworkCore.Metadata.IForeignKey userForeignKey = entityType.GetForeignKeys().Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual([nameof(ScoreModel.UserId)], StringComparer.Ordinal));
        Assert.That(userForeignKey.PrincipalEntityType.ClrType, Is.EqualTo(typeof(User)));
        Assert.That(userForeignKey.GetConstraintName(), Is.EqualTo("scores_ibfk_2"));
        Assert.That(userForeignKey.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict));
    }

    private static void AssertIndexName(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, IReadOnlyCollection<string> propertyNames, string expectedName)
    {
        Microsoft.EntityFrameworkCore.Metadata.IIndex index = entityType.GetIndexes().Single(i => i.Properties.Select(p => p.Name).SequenceEqual(propertyNames, StringComparer.Ordinal));
        Assert.That(index.GetDatabaseName(), Is.EqualTo(expectedName));
    }

    private static ScoreModel CreateScore(
        long id = 100,
        int beatmapId = 200,
        int userId = 300,
        int mode = 0,
        string checksum = "0123456789abcdef0123456789abcdef",
        double pp = 250,
        bool hasReplay = true,
        DateTimeOffset? endedAt = null)
    {
        return new ScoreModel
        {
            Id = id,
            Accuracy = 0.9876,
            BeatmapChecksum = checksum,
            ClassicTotalScore = 123456789,
            ClassicTotalScoreWithoutMods = 120000000,
            EndedAt = endedAt ?? new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero),
            HasReplay = hasReplay,
            MaxCombo = 1234,
            Mods =
            [
                new APIMod { Acronym = "HD" },
            ],
            PlaylistItemId = 77,
            Pp = pp,
            Rank = ScoreRank.S,
            RoomId = 88,
            StartedAt = new DateTimeOffset(2026, 5, 17, 11, 58, 0, TimeSpan.Zero),
            TotalScore = 987654321,
            TotalScoreWithoutMods = 876543210,
            Type = "solo_score",
            BeatmapId = beatmapId,
            UserId = userId,
            Mode = mode,
            Processed = true,
            Ranked = true,
            Statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 320,
                [HitResult.Ok] = 45,
                [HitResult.Meh] = 6,
                [HitResult.Miss] = 1,
                [HitResult.Perfect] = 12,
                [HitResult.Good] = 8,
                [HitResult.LargeTickMiss] = 2,
                [HitResult.LargeTickHit] = 15,
                [HitResult.SliderTailHit] = 16,
                [HitResult.SmallTickHit] = 25,
            },
            MaximumStatistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 400,
                [HitResult.Ok] = 60,
                [HitResult.Meh] = 10,
                [HitResult.Miss] = 2,
            },
        };
    }
}