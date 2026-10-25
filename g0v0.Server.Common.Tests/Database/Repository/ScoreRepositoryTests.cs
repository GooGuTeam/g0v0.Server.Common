// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using MySqlDbContext = g0v0.Server.Common.Database.MySQL.MysqlDbContext;
using MySqlScoreRepository = g0v0.Server.Common.Database.MySQL.Repository.ScoreRepository;
using PostgreSqlDbContext = g0v0.Server.Common.Database.PostgreSQL.PostgreSqlDbContext;
using PostgreSqlScoreRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.ScoreRepository;
using ScoreModel = g0v0.Server.Common.Database.Models.Score;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="IScoreRepository"/> contract against both database backends.
/// </summary>
[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class ScoreRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IScoreRepository _repository = null!;

    private DbSet<ScoreModel> Scores => _context.Set<ScoreModel>();

    private DbSet<ScoreToken> ScoreTokens => _context.Set<ScoreToken>();

    private DbSet<LeaderboardScore> LeaderboardScores => _context.Set<LeaderboardScore>();

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        _repository = backend == RepositoryBackend.MySql
            ? new MySqlScoreRepository((MySqlDbContext)_context)
            : new PostgreSqlScoreRepository((PostgreSqlDbContext)_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetPassingScoresForRoomAsync_FiltersRoomPlaylistAndFailedScores()
    {
        ScoreModel passing = CreateScore(id: 1);
        passing.RoomId = 42;
        passing.Passed = true;
        ScoreModel failed = CreateScore(id: 2);
        failed.RoomId = 42;
        failed.Passed = false;
        ScoreModel otherRoom = CreateScore(id: 3);
        otherRoom.RoomId = 43;
        otherRoom.Passed = true;
        ScoreModel noPlaylist = CreateScore(id: 4);
        noPlaylist.RoomId = 42;
        noPlaylist.Passed = true;
        noPlaylist.PlaylistItemId = null;
        Scores.AddRange(passing, failed, otherRoom, noPlaylist);
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetPassingScoresForRoomAsync(42);

        Assert.That(results.Select(score => score.Id), Is.EqualTo(new long[] { 1 }));
    }

    [Test]
    public async Task GetByIdAsync_WhenScoreExists_ShouldReturnScore()
    {
        ScoreModel score = CreateScore(id: 111);
        Scores.Add(score);
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
        Scores.AddRange(
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
        Scores.AddRange(
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
        Scores.AddRange(
            CreateScore(id: 1, userId: 7, mode: 3, endedAt: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateScore(id: 2, userId: 7, mode: 3, endedAt: new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero)),
            CreateScore(id: 3, userId: 7, mode: 0, endedAt: new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero)));
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetRecentByUserIdAndModeAsync(7, 3, 2);

        Assert.That(results.Select(s => s.Id), Is.EqualTo(new long[] { 2, 1 }));
    }

    [Test]
    public async Task GetBestByUserIdAndModeAsync_ShouldReturnOrderedLimitedScores()
    {
        Scores.AddRange(
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
        Scores.Add(score);
        ScoreTokens.Add(new ScoreToken
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
        ScoreTokens.Add(new ScoreToken
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
        Scores.Add(CreateScore(id: 700, hasReplay: false));
        await _context.SaveChangesAsync();

        await _repository.MarkScoreHasReplay(700);

        bool hasReplay = await Scores
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

        ScoreModel? persisted = await Scores.SingleOrDefaultAsync(s => s.Id == 1234);
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
        Scores.Add(score);
        await _context.SaveChangesAsync();

        score.Pp = 420;
        score.Rank = ScoreRank.XH;
        score.Statistics[HitResult.Miss] = 0;
        score.Statistics.Remove(HitResult.LargeTickMiss);
        score.MaximumStatistics[HitResult.Perfect] = 99;
        await _repository.UpdateAsync(score);

        ScoreModel? updated = await Scores.SingleOrDefaultAsync(s => s.Id == 4321);
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
        Scores.Add(score);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(score);

        bool exists = await Scores.AnyAsync(s => s.Id == 999);
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task GetBeatmapLeaderboardAsync_KeepsTheBestScorePerUserAndOrdersItDescending()
    {
        ScoreModel aliceBest = CreateScore(id: 1, userId: 10, endedAt: DateTimeOffset.UtcNow);
        aliceBest.TotalScore = 900;
        aliceBest.Passed = true;
        ScoreModel aliceLower = CreateScore(id: 2, userId: 10, endedAt: DateTimeOffset.UtcNow);
        aliceLower.TotalScore = 500;
        aliceLower.Passed = true;
        ScoreModel bobBest = CreateScore(id: 3, userId: 11, endedAt: DateTimeOffset.UtcNow);
        bobBest.TotalScore = 700;
        bobBest.Passed = true;
        ScoreModel failed = CreateScore(id: 4, userId: 12, endedAt: DateTimeOffset.UtcNow);
        failed.TotalScore = 5000;
        failed.Passed = false;
        AddLeaderboardEntry(aliceBest);
        AddLeaderboardEntry(aliceLower);
        AddLeaderboardEntry(bobBest);
        Scores.AddRange(aliceBest, aliceLower, bobBest, failed);
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> leaderboard = await _repository.GetBeatmapLeaderboardAsync(200, 0, limit: 50);

        Assert.That(leaderboard.Select(score => score.Id), Is.EqualTo(new long[] { 1, 3 }));
    }

    [Test]
    public async Task GetBeatmapLeaderboardAsync_LegacyOnlyExcludesScoresWithoutALegacyValue()
    {
        ScoreModel stable = CreateScore(id: 1, userId: 10, endedAt: DateTimeOffset.UtcNow);
        stable.TotalScore = 900;
        stable.Passed = true;
        ScoreModel lazer = CreateScore(id: 2, userId: 11, endedAt: DateTimeOffset.UtcNow);
        lazer.TotalScore = 5000;
        lazer.Passed = true;
        lazer.ClassicTotalScore = 0;
        AddLeaderboardEntry(stable);
        AddLeaderboardEntry(lazer);
        Scores.AddRange(stable, lazer);
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> all = await _repository.GetBeatmapLeaderboardAsync(200, 0, limit: 50);
        IReadOnlyList<ScoreModel> legacy = await _repository.GetBeatmapLeaderboardAsync(200, 0, limit: 50, legacyOnly: true);

        Assert.Multiple(() =>
        {
            Assert.That(all.Select(score => score.Id), Is.EqualTo(new long[] { 2, 1 }));
            Assert.That(legacy.Select(score => score.Id), Is.EqualTo(new long[] { 1 }));
        });
    }

    [Test]
    public async Task GetBeatmapLeaderboardAsync_FiltersByModsAndFriends()
    {
        ScoreModel hidden = CreateScore(id: 1, userId: 10, endedAt: DateTimeOffset.UtcNow);
        hidden.TotalScore = 900;
        hidden.Passed = true;
        hidden.Mods = [new APIMod { Acronym = "HD" }];
        ScoreModel nomod = CreateScore(id: 2, userId: 11, endedAt: DateTimeOffset.UtcNow);
        nomod.TotalScore = 800;
        nomod.Passed = true;
        nomod.Mods = [];
        AddLeaderboardEntry(hidden);
        AddLeaderboardEntry(nomod);
        Scores.AddRange(hidden, nomod);
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> withHd = await _repository.GetBeatmapLeaderboardAsync(200, 0, limit: 50, modAcronyms: ["HD"]);
        IReadOnlyList<ScoreModel> friends = await _repository.GetBeatmapLeaderboardAsync(200, 0, limit: 50, userIds: [11]);

        Assert.Multiple(() =>
        {
            Assert.That(withHd.Select(score => score.Id), Is.EqualTo(new long[] { 1 }));
            Assert.That(friends.Select(score => score.Id), Is.EqualTo(new long[] { 2 }));
        });
    }

    [Test]
    public async Task LeaderboardCountBestAndRankAgreeWithTheRows()
    {
        ScoreModel best = CreateScore(id: 1, userId: 10, endedAt: DateTimeOffset.UtcNow);
        best.TotalScore = 900;
        best.Passed = true;
        ScoreModel worse = CreateScore(id: 2, userId: 11, endedAt: DateTimeOffset.UtcNow);
        worse.TotalScore = 700;
        worse.Passed = true;
        AddLeaderboardEntry(best);
        AddLeaderboardEntry(worse);
        Scores.AddRange(best, worse);
        await _context.SaveChangesAsync();

        int count = await _repository.CountBeatmapLeaderboardAsync(200, 0);
        ScoreModel? personalBest = await _repository.GetBestBeatmapScoreAsync(200, 0, 11);
        int rank = await _repository.GetBeatmapLeaderboardRankAsync(200, 0, 11);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(personalBest?.Id, Is.EqualTo(2));
            Assert.That(rank, Is.EqualTo(2));
        });
    }

    private static ScoreModel CreateScore(
        long id,
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

    private void AddLeaderboardEntry(ScoreModel score) => LeaderboardScores.Add(new LeaderboardScore
    {
        ScoreId = score.Id,
        UserId = score.UserId,
        BeatmapId = score.BeatmapId,
        Mode = score.Mode,
        TotalScore = score.TotalScore,
        Mods = score.Mods.Select(mod => mod.Acronym).ToList(),
        Rank = score.Rank
    });
}