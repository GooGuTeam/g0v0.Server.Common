// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using ScoreModel = g0v0.Server.Common.Database.Models.Score;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlScoreRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private ScoreRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PostgreSqlDbContext> options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new ScoreRepository(_context);
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
        _context.Scores.AddRange(passing, failed, otherRoom, noPlaylist);
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetPassingScoresForRoomAsync(42);

        Assert.That(results.Select(score => score.Id), Is.EqualTo(new long[] { 1 }));
    }

    [Test]
    public async Task GetRecentByUserIdAndModeAsync_ShouldReturnOrderedLimitedScores()
    {
        _context.Scores.AddRange(
            CreateScore(id: 1, userId: 7, mode: 3, endedAt: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateScore(id: 2, userId: 7, mode: 3, endedAt: new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero)),
            CreateScore(id: 3, userId: 7, mode: 0, endedAt: new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero)));
        await _context.SaveChangesAsync();

        IReadOnlyList<ScoreModel> results = await _repository.GetRecentByUserIdAndModeAsync(7, 3, 2);

        Assert.That(results.Select(score => score.Id), Is.EqualTo(new long[] { 2, 1 }));
    }

    [Test]
    public async Task GetScoreByToken_WhenTokenHasScore_ShouldReturnScore()
    {
        ScoreModel score = CreateScore(id: 42);
        _context.Scores.Add(score);
        _context.ScoreTokens.Add(new ScoreToken
        {
            Id = 4242,
            ScoreId = score.Id,
            Score = score,
            UserId = score.UserId,
            BeatmapId = score.BeatmapId,
            RulesetId = score.Mode,
        });
        await _context.SaveChangesAsync();

        ScoreModel? result = await _repository.GetScoreByToken(4242);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(42));
    }

    [Test]
    public async Task MarkScoreHasReplay_WhenScoreExists_ShouldSetHasReplay()
    {
        _context.Scores.Add(CreateScore(id: 55, hasReplay: false));
        await _context.SaveChangesAsync();

        await _repository.MarkScoreHasReplay(55);

        bool hasReplay = await _context.Scores
            .Where(score => score.Id == 55)
            .Select(score => score.HasReplay)
            .SingleAsync();
        Assert.That(hasReplay, Is.True);
    }

    [Test]
    public void ScoreRepository_ShouldImplementIPostgreSqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IPostgreSqlRepository>());
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
        _context.Scores.AddRange(aliceBest, aliceLower, bobBest, failed);
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
        _context.Scores.AddRange(stable, lazer);
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
        _context.Scores.AddRange(hidden, nomod);
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
        _context.Scores.AddRange(best, worse);
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
        int userId = 300,
        int mode = 0,
        bool hasReplay = true,
        DateTimeOffset? endedAt = null) => new()
        {
            Id = id,
            Accuracy = 0.9876,
            BeatmapChecksum = "0123456789abcdef0123456789abcdef",
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
            Pp = 250,
            Rank = ScoreRank.S,
            RoomId = 88,
            StartedAt = new DateTimeOffset(2026, 5, 17, 11, 58, 0, TimeSpan.Zero),
            TotalScore = 987654321,
            TotalScoreWithoutMods = 876543210,
            Type = "solo_score",
            BeatmapId = 200,
            UserId = userId,
            Mode = mode,
            Processed = true,
            Ranked = true,
            Passed = true,
            Statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 320,
            },
            MaximumStatistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 400,
            },
        };

    private void AddLeaderboardEntry(ScoreModel score) => _context.LeaderboardScores.Add(new LeaderboardScore
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