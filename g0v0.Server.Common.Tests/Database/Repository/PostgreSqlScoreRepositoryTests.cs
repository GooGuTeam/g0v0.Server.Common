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
        var options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
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
        var score = CreateScore(id: 42);
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
}