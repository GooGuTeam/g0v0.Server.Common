// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Rulesets;
using g0v0.Server.Common.Utils;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace g0v0.Server.Common.Tests.Utils;

public class ScoreAttributeUtilsTests
{
    private readonly RulesetManager _rulesets = new(Path.Combine(Path.GetTempPath(), "nonexistent-rulesets"));

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void CalculatesNativeDifficultyAndPerformance(int mode)
    {
        ScoreAttributeUtils utils = new(_rulesets);
        FlatWorkingBeatmap working = ScoreAttributeUtils.DecodeBeatmap(BeatmapText(mode));
        Ruleset ruleset = _rulesets.GetRuleset(mode);
        DifficultyAttributes difficulty = utils.CalculateDifficultyAttributes(working, mode, []);
        IBeatmap playable = working.GetPlayableBeatmap(ruleset.RulesetInfo, []);
        using ScoreProcessor processor = ruleset.CreateScoreProcessor();
        processor.ApplyBeatmap(playable);
        ScoreInfo info = new()
        {
            Ruleset = ruleset.RulesetInfo,
            Accuracy = 1,
            MaxCombo = difficulty.MaxCombo,
            Statistics = processor.MaximumStatistics,
            MaximumStatistics = processor.MaximumStatistics,
            TotalScore = 1000000
        };
        PerformanceAttributes performance = utils.CalculatePerformanceAttributes(info, difficulty);
        Assert.Multiple(() =>
        {
            Assert.That(difficulty.StarRating, Is.GreaterThanOrEqualTo(0));
            Assert.That(difficulty.MaxCombo, Is.GreaterThan(0));
            Assert.That(performance.Total, Is.GreaterThanOrEqualTo(0).And.LessThan(double.PositiveInfinity));
        });
    }

    [TestCase(0, LegacyMods.DoubleTime, 1.12)]
    [TestCase(0, LegacyMods.HardRock, 1.06)]
    [TestCase(0, LegacyMods.Hidden | LegacyMods.DoubleTime, 1.1872)]
    [TestCase(0, LegacyMods.HalfTime, 0.3)]
    [TestCase(0, LegacyMods.Relax, 0)]
    [TestCase(1, LegacyMods.DoubleTime, 1.12)]
    [TestCase(2, LegacyMods.DoubleTime, 1.06)]
    [TestCase(3, LegacyMods.DoubleTime, 1)]
    [TestCase(3, LegacyMods.HalfTime, 0.5)]
    public void UsesStableMultipliers(int mode, LegacyMods legacy, double expected)
    {
        ScoreAttributeUtils utils = new(_rulesets);
        FlatWorkingBeatmap working = ScoreAttributeUtils.DecodeBeatmap(BeatmapText(mode));
        Mod[] mods = _rulesets.GetRuleset(mode).ConvertFromLegacyMods(legacy).ToArray();
        Assert.That(utils.GetLegacyScoreMultiplier(working.Beatmap, mode, mods), Is.EqualTo(expected).Within(0.000001));
    }

    private static string BeatmapText(int mode) => $"""
        osu file format v14

        [General]
        AudioFilename: test.mp3
        Mode: {mode}

        [Metadata]
        Title:Test
        Artist:Test
        Creator:Test
        Version:Test

        [Difficulty]
        HPDrainRate:5
        CircleSize:4
        OverallDifficulty:5
        ApproachRate:5
        SliderMultiplier:1.4
        SliderTickRate:1

        [TimingPoints]
        0,500,4,2,1,100,1,0

        [HitObjects]
        64,192,1000,1,0,0:0:0:0:
        192,192,1500,1,0,0:0:0:0:
        320,192,2000,1,0,0:0:0:0:
        448,192,2500,1,0,0:0:0:0:
        """;
}