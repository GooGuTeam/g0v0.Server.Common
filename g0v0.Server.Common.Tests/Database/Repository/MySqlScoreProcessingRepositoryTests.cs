// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.MySQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Rulesets.Scoring;

namespace g0v0.Server.Common.Tests.Database.Repository;

public class MySqlScoreProcessingRepositoryTests
{
    [Test]
    public async Task ReadsUnprocessedScoresByTypeInIdOrderAndSupportsPagination()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Scores.AddRange(
            new Score { Id = 1, Type = "stable", HasReplay = true, Passed = true, UserId = 2 },
            new Score { Id = 2, Type = "stable", Passed = false },
            new Score { Id = 3, Type = "stable", Passed = true, HasReplay = false },
            new Score { Id = 4, Type = "stable", Processed = true },
            new Score { Id = 5, Type = "lazer" });
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);
        Assert.That((await repository.GetUnprocessedScoresAsync(0, 100, "stable")).Select(s => s.Id), Is.EqualTo(new long[] { 1, 2, 3 }));
        Assert.That((await repository.GetUnprocessedScoresAsync(1, 1, "stable")).Single().Id, Is.EqualTo(2));
        Assert.That(await repository.GetUnprocessedScoresAsync(0, 0, "stable"), Is.Empty);
    }

    [TestCase(1)]
    [TestCase(6)]
    public async Task PersistsTaikoStrongHitStatisticsInBothNormalAndRelaxModes(int mode)
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Scores.Add(new Score { Id = 7, Type = "stable", Mode = mode, Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 10, [HitResult.LargeBonus] = 4 } });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Score saved = await context.Scores.SingleAsync();
        Assert.That(saved.Mode, Is.EqualTo(mode));
        Assert.That(saved.Statistics[HitResult.LargeBonus], Is.EqualTo(4));
    }

    [Test]
    public async Task CompletesCalculatedFieldsWithoutOverwritingReplayState()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Scores.Add(new Score { Id = 10, Type = "stable", HasReplay = true });
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);
        await repository.CompleteProcessingAsync(new Score { Id = 10, HasReplay = false, Pp = 123.45, Accuracy = 0.98, ClassicTotalScoreWithoutMods = 1000, Passed = true });
        await repository.CompleteProcessingAsync(new Score { Id = 10, Passed = true, Pp = 999 }, 100);
        Assert.That((await context.UserStatistics.SingleAsync()).PlayCount, Is.EqualTo(1));
        context.ChangeTracker.Clear();
        Score saved = await context.Scores.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(saved.HasReplay, Is.True);
            Assert.That(saved.Processed, Is.True);
            Assert.That(saved.Pp, Is.EqualTo(123.45));
            Assert.That(saved.ClassicTotalScoreWithoutMods, Is.EqualTo(1000));
            Assert.That(saved.Accuracy, Is.EqualTo(0.98));
        });
    }

    [Test]
    public async Task CreatesThePerformanceBestAndTheLeaderboardEntryOfAPassedRankedScore()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Beatmaps.Add(new Beatmap { Id = 200, BeatmapSetId = 300, Status = osu.Game.Beatmaps.BeatmapOnlineStatus.Ranked });
        context.Scores.Add(SeededScore(10));
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);
        await repository.CompleteProcessingAsync(CompletedScore(10, totalScore: 900, pp: 42.5), 120);

        BestScore best = await context.BestScores.SingleAsync();
        LeaderboardScore entry = await context.LeaderboardScores.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(best.ScoreId, Is.EqualTo(10));
            Assert.That(best.Pp, Is.EqualTo(42.5f));
            Assert.That(entry.ScoreId, Is.EqualTo(10));
            Assert.That(entry.TotalScore, Is.EqualTo(900));
            Assert.That(entry.Mods, Is.EquivalentTo(new List<string> { "HD" }));
            Assert.That(entry.Rank, Is.EqualTo(global::osu.Game.Scoring.ScoreRank.S));
        });
    }

    [Test]
    public async Task ReplacesTheLeaderboardEntryWithAHigherScoreOfTheSameModSet()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Beatmaps.Add(new Beatmap { Id = 200, BeatmapSetId = 300, Status = osu.Game.Beatmaps.BeatmapOnlineStatus.Ranked });
        context.Scores.AddRange(
            SeededScore(10),
            SeededScore(11));
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);
        await repository.CompleteProcessingAsync(CompletedScore(10, totalScore: 500, pp: 20), 120);
        await repository.CompleteProcessingAsync(CompletedScore(11, totalScore: 900, pp: 40), 120);

        LeaderboardScore entry = await context.LeaderboardScores.SingleAsync();
        Assert.That(entry.ScoreId, Is.EqualTo(11));
        Assert.That(entry.TotalScore, Is.EqualTo(900));
    }

    [Test]
    public async Task KeepsOneLeaderboardEntryPerModSetAndDropsTheDisplacedOverallBest()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Beatmaps.Add(new Beatmap { Id = 200, BeatmapSetId = 300, Status = osu.Game.Beatmaps.BeatmapOnlineStatus.Ranked });
        context.Scores.AddRange(
            SeededScore(10),
            SeededScore(11, mods: []),
            SeededScore(12));
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);

        // A score of another mod set is kept next to the existing one when it is not the new best.
        await repository.CompleteProcessingAsync(CompletedScore(10, totalScore: 900, pp: 40), 120);
        await repository.CompleteProcessingAsync(CompletedScore(11, totalScore: 500, pp: 20, mods: []), 120);
        Assert.That((await context.LeaderboardScores.ToListAsync()).Select(entry => entry.ScoreId), Is.EquivalentTo(new long[] { 10, 11 }));

        // A better score of an existing mod set replaces only that entry.
        await repository.CompleteProcessingAsync(CompletedScore(12, totalScore: 950, pp: 45), 120);
        List<LeaderboardScore> entries = await context.LeaderboardScores.OrderBy(entry => entry.ScoreId).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(entry => entry.ScoreId), Is.EqualTo(new long[] { 11, 12 }));
            Assert.That(entries.Single(entry => entry.ScoreId == 12).TotalScore, Is.EqualTo(950));
        });
    }

    [Test]
    public async Task SkipsLeaderboardEntriesForBeatmapsWithoutALeaderboard()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Beatmaps.Add(new Beatmap { Id = 200, BeatmapSetId = 300, Status = osu.Game.Beatmaps.BeatmapOnlineStatus.Pending });
        context.Scores.Add(SeededScore(10));
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);
        await repository.CompleteProcessingAsync(CompletedScore(10, totalScore: 900, pp: 40), 120);

        Assert.That(await context.LeaderboardScores.ToListAsync(), Is.Empty);
        Assert.That((await context.BestScores.SingleAsync()).ScoreId, Is.EqualTo(10));
    }

    [Test]
    public async Task CountsOnePlayPerValidScoreOnTheBeatmapAndTheMonth()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Beatmaps.Add(new Beatmap { Id = 200, BeatmapSetId = 300, Status = osu.Game.Beatmaps.BeatmapOnlineStatus.Ranked });
        context.Scores.AddRange(
            SeededScore(10),
            SeededScore(11, passed: false));
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);
        await repository.CompleteProcessingAsync(CompletedScore(10, totalScore: 900, pp: 40), 120);
        await repository.CompleteProcessingAsync(CompletedScore(11, totalScore: 0, pp: 0, passed: false), 1);

        BeatmapPlaycount beatmapPlaycount = await context.BeatmapPlaycounts.SingleAsync();
        MonthlyPlaycount monthlyPlaycount = await context.MonthlyPlaycounts.SingleAsync();
        UserStatistics statistics = await context.UserStatistics.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(beatmapPlaycount.Playcount, Is.EqualTo(1));
            Assert.That(monthlyPlaycount.Count, Is.EqualTo(1));
            Assert.That(statistics.PlayCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task AccumulatesPerformancePointsPlayTimeAndCountersAcrossScores()
    {
        await using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Beatmaps.AddRange(
            new Beatmap { Id = 200, BeatmapSetId = 300, Status = osu.Game.Beatmaps.BeatmapOnlineStatus.Ranked },
            new Beatmap { Id = 201, BeatmapSetId = 300, Status = osu.Game.Beatmaps.BeatmapOnlineStatus.Ranked });
        context.Scores.AddRange(SeededScore(10), SeededScore(11, beatmapId: 201));
        await context.SaveChangesAsync();
        ScoreRepository repository = new(context);

        // The two plays are on different difficulties: performance is kept per beatmap, so both
        // contribute to the weighted total.
        await repository.CompleteProcessingAsync(CompletedScore(10, totalScore: 900, pp: 40), 60);
        await repository.CompleteProcessingAsync(CompletedScore(11, totalScore: 950, pp: 50, beatmapId: 201), 120);

        UserStatistics statistics = await context.UserStatistics.SingleAsync();
        Assert.Multiple(() =>
        {
            // Weighted pp of the two beatmap bests: 50 + 40 * 0.95.
            Assert.That(statistics.Pp, Is.EqualTo(88).Within(0.0001));
            Assert.That(statistics.PlayTime, Is.EqualTo(180));
            Assert.That(statistics.PlayCount, Is.EqualTo(2));
            Assert.That(statistics.TotalScore, Is.EqualTo(1850));
        });
    }

    private static Score SeededScore(long id, bool passed = true, IList<osu.Game.Online.API.APIMod>? mods = null, int beatmapId = 200) => new()
    {
        Id = id,
        Type = "stable",
        UserId = 400,
        BeatmapId = beatmapId,
        Mode = 0,
        Passed = passed,
        Mods = mods ?? [new osu.Game.Online.API.APIMod { Acronym = "HD" }],
        EndedAt = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero),
        StartedAt = new DateTimeOffset(2026, 5, 17, 11, 58, 0, TimeSpan.Zero),
        Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 300 },
        MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 300 }
    };

    private static Score CompletedScore(
        long id,
        int totalScore,
        double pp,
        bool passed = true,
        IList<osu.Game.Online.API.APIMod>? mods = null,
        int beatmapId = 200) => new()
        {
            Id = id,
            UserId = 400,
            BeatmapId = beatmapId,
            Mode = 0,
            Type = "stable",
            Passed = passed,
            Ranked = true,
            Pp = pp,
            TotalScore = totalScore,
            ClassicTotalScore = totalScore,
            Accuracy = 0.98,
            Rank = global::osu.Game.Scoring.ScoreRank.S,
            MaxCombo = 500,
            EndedAt = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero),
            StartedAt = new DateTimeOffset(2026, 5, 17, 11, 58, 0, TimeSpan.Zero),
            Mods = mods ?? [new osu.Game.Online.API.APIMod { Acronym = "HD" }],
            Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 300 },
            MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 300 }
        };
}