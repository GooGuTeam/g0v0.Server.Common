// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Utils;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using Score = g0v0.Server.Common.Database.Models.Score;

namespace g0v0.Server.Common.Tests.Utils;

public class UserScoreStatisticsUtilsTests
{
    [TestCase(0, "RX", true, false, 4)]
    [TestCase(1, "RX", true, false, 6)]
    [TestCase(2, "RX", true, false, 7)]
    [TestCase(3, "RX", true, false, 3)]
    [TestCase(0, "AP", false, true, 5)]
    [TestCase(0, "RX", false, false, 0)]
    [TestCase(0, "AP", false, false, 0)]
    [TestCase(0, "RX", false, true, 0)]
    public void SeparatesOnlyEnabledSpecialModes(int mode, string mod, bool relax, bool autopilot, int expected)
    {
        APIMod[] mods = [new() { Acronym = mod }];
        GameConfig config = new() { EnableRelax = relax, EnableAutopilot = autopilot };
        Assert.That(ScoreModeUtils.GetScoreMode(mode, mods, config), Is.EqualTo(expected));
    }

    [Test]
    public void AwardsPerformanceToRankedModsAndRejectsUnrankedOnes()
    {
        GameConfig config = new();
        Assert.Multiple(() =>
        {
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [], config, legacyScore: false), Is.True);
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [new global::osu.Game.Rulesets.Osu.Mods.OsuModHidden()], config, legacyScore: false), Is.True);
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [new global::osu.Game.Rulesets.Osu.Mods.OsuModAutoplay()], config, legacyScore: false), Is.False);
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [new global::osu.Game.Rulesets.Osu.Mods.OsuModRelax()], config, legacyScore: false), Is.False);
        });
    }

    [Test]
    public void ExemptsTheInjectedClassicModOnlyForLegacyScores()
    {
        GameConfig config = new();
        global::osu.Game.Rulesets.Osu.Mods.OsuModClassic classic = new();
        Assert.Multiple(() =>
        {
            Assert.That(classic.Ranked, Is.False);
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [classic], config, legacyScore: true), Is.True);
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [classic], config, legacyScore: false), Is.False);
        });
    }

    [Test]
    public void ExemptsRelaxAndAutopilotOnlyWhenTheirFeatureIsEnabled()
    {
        global::osu.Game.Rulesets.Osu.Mods.OsuModRelax relax = new();
        global::osu.Game.Rulesets.Osu.Mods.OsuModAutopilot autopilot = new();
        GameConfig enabled = new() { EnableRelax = true, EnableAutopilot = true };
        GameConfig disabled = new();
        Assert.Multiple(() =>
        {
            Assert.That(relax.Ranked, Is.False);
            Assert.That(autopilot.Ranked, Is.False);
            Assert.That(ScoreModeUtils.CanAwardPerformance(4, [relax], enabled, legacyScore: false), Is.True);
            Assert.That(ScoreModeUtils.CanAwardPerformance(5, [autopilot], enabled, legacyScore: false), Is.True);
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [relax], enabled, legacyScore: false), Is.False);
            Assert.That(ScoreModeUtils.CanAwardPerformance(0, [autopilot], enabled, legacyScore: false), Is.False);
            Assert.That(ScoreModeUtils.CanAwardPerformance(4, [relax], disabled, legacyScore: false), Is.False);
            Assert.That(ScoreModeUtils.CanAwardPerformance(5, [autopilot], disabled, legacyScore: false), Is.False);
        });
    }

    [TestCase(4, "OSURX", 0)]
    [TestCase(5, "OSUAP", 0)]
    [TestCase(6, "TAIKORX", 1)]
    [TestCase(7, "FRUITSRX", 2)]
    public void LegacyDatabaseRoundTripsSpecialModes(int mode, string legacy, int baseMode)
    {
        using MysqlDbContext context = new(new DbContextOptionsBuilder<MysqlDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter converter = context.Model.FindEntityType(typeof(Score))!.FindProperty(nameof(Score.Mode))!.GetValueConverter()!;
        Assert.That(converter.ConvertToProvider(mode), Is.EqualTo(legacy));
        Assert.That(converter.ConvertFromProvider(legacy), Is.EqualTo(mode));
        Assert.That(ScoreModeUtils.GetBaseRulesetId(mode), Is.EqualTo(baseMode));
    }

    [Test]
    public void AggregatesSeparatePpAndScoreBestsAndWeightsAccuracyInPercent()
    {
        UserStatistics statistics = new() { UserId = 1, Mode = 4 };
        Score older = MakeScore(1, 1, 4, 100, 0.9, 800000, ScoreRank.A);
        Score otherMap = MakeScore(2, 2, 4, 90, 0.95, 700000, ScoreRank.S);
        Score incoming = MakeScore(3, 1, 4, 110, 0.99, 600000, ScoreRank.X);
        incoming.Statistics[HitResult.Great] = 10;
        incoming.Statistics[HitResult.Ok] = 1;
        Score excludedMode = MakeScore(4, 3, 0, 999, 1, 1000000, ScoreRank.X);
        Score failed = MakeScore(5, 4, 4, 999, 1, 1000000, ScoreRank.F);
        failed.Passed = false;
        UserScoreStatisticsUtils.Apply(statistics, incoming, [older, otherMap, excludedMode, failed], 20);
        Assert.Multiple(() =>
        {
            Assert.That(statistics.Pp, Is.EqualTo(110 + (90 * 0.95)).Within(0.000001));
            Assert.That(statistics.HitAccuracy, Is.EqualTo((0.99 + (0.95 * 0.95)) / 1.95 * 100).Within(0.000001));
            Assert.That(statistics.RankedScore, Is.EqualTo(1500000));
            Assert.That(statistics.GradeA, Is.EqualTo(1));
            Assert.That(statistics.GradeS, Is.EqualTo(1));
            Assert.That(statistics.GradeSs, Is.Zero);
            Assert.That(statistics.TotalScore, Is.EqualTo(600000));
            Assert.That(statistics.PlayCount, Is.EqualTo(1));
            Assert.That(statistics.PlayTime, Is.EqualTo(20));
            Assert.That(statistics.Count300, Is.EqualTo(10));
            Assert.That(statistics.TotalHits, Is.EqualTo(11));
            Assert.That(statistics.LevelCurrent, Is.GreaterThan(1));
        });
    }

    [Test]
    public void FailedScoreAddsLifetimeCountersWithoutReplacingBestScores()
    {
        UserStatistics statistics = new() { UserId = 1, Mode = 0 };
        Score best = MakeScore(1, 1, 0, 100, 0.99, 900000, ScoreRank.S);
        Score failed = MakeScore(2, 1, 0, 0, 0.8, 1000, ScoreRank.F);
        failed.Passed = false;
        UserScoreStatisticsUtils.Apply(statistics, failed, [best], 1);
        Assert.Multiple(() =>
        {
            Assert.That(statistics.Pp, Is.EqualTo(100));
            Assert.That(statistics.RankedScore, Is.EqualTo(900000));
            Assert.That(statistics.TotalScore, Is.EqualTo(1000));
            Assert.That(statistics.PlayCount, Is.Zero);
        });
    }

    private static Score MakeScore(long id, int beatmapId, int mode, double pp, double accuracy, int totalScore, ScoreRank rank)
        => new() { Id = id, BeatmapId = beatmapId, UserId = 1, Mode = mode, Pp = pp, Accuracy = accuracy, TotalScore = totalScore, Rank = rank, Ranked = true, Passed = true, Processed = true };
}