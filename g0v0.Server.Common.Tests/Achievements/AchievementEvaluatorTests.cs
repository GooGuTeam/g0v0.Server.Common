// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Achievements;
using g0v0.Server.Common.Database.Models;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using Beatmap = g0v0.Server.Common.Database.Models.Beatmap;
using Score = g0v0.Server.Common.Database.Models.Score;

namespace g0v0.Server.Common.Tests.Achievements;

public class AchievementEvaluatorTests
{
    [Test]
    public void CataloguePreservesSharedIdsAndUniqueIcons()
    {
        Assert.That(AchievementEvaluator.Definitions, Has.Count.EqualTo(129));
        Assert.That(AchievementEvaluator.Definitions.Select(item => item.Id), Is.Unique);
        Assert.That(AchievementEvaluator.Definitions.Select(item => item.Icon), Is.Unique);
        Assert.That(AchievementEvaluator.Definitions.Single(item => item.Id == 1).Icon, Is.EqualTo("osu-skill-pass-1"));
        Assert.That(AchievementEvaluator.Definitions.Single(item => item.Id == 89).Name, Is.EqualTo("Finality"));
    }

    [TestCase(0, "osu")]
    [TestCase(1, "taiko")]
    [TestCase(2, "fruits")]
    [TestCase(3, "mania")]
    public void SkillAwardsExactlyTheModAdjustedStarBandAndMode(int mode, string iconMode)
    {
        AchievementEvaluationContext context = Context();
        context.Score.Mode = mode;
        context.Difficulty.StarRating = 5.999;
        string[] skills = Evaluate(context).Where(icon => icon.Contains("skill-", StringComparison.Ordinal)).ToArray();
        Assert.That(skills, Is.EqualTo((string[])[iconMode + "-skill-pass-5"]));
        context.Difficulty.StarRating = 6;
        Assert.That(Evaluate(context), Does.Contain(iconMode + "-skill-pass-6"));
        Assert.That(Evaluate(context), Does.Not.Contain(iconMode + "-skill-pass-5"));
    }

    [Test]
    public void FullComboUsesCalculatedMaximumAndRejectsMisses()
    {
        AchievementEvaluationContext context = Context();
        context.Beatmap.MaxCombo = 1000;
        context.Score.MaxCombo = context.Difficulty.MaxCombo;
        Assert.That(Evaluate(context), Does.Contain("osu-skill-fc-5"));
        context.Score.Statistics[HitResult.Miss] = 1;
        Assert.That(Evaluate(context), Does.Not.Contain("osu-skill-fc-5"));
    }

    [TestCase("NF")]
    [TestCase("EZ")]
    [TestCase("HT")]
    public void DifficultyReductionPreventsSkillMedalsButStillAllowsIntroMedals(string acronym)
    {
        AchievementEvaluationContext context = Context(acronym);
        string[] icons = Evaluate(context);
        Assert.That(icons.Any(icon => icon.Contains("skill-", StringComparison.Ordinal)), Is.False);
        Assert.That(icons.Any(icon => icon.StartsWith("all-intro-", StringComparison.Ordinal)), Is.True);
    }

    [TestCase("AT")]
    [TestCase("RX")]
    [TestCase("AP")]
    [TestCase("CN")]
    public void AutomatedScoresCannotUnlockMedals(string acronym)
        => Assert.That(Evaluate(Context(acronym)), Is.Empty);

    [Test]
    public void FailedAndUnprocessedScoresCannotUnlockSkillOrModMedals()
    {
        AchievementEvaluationContext context = Context("HD");
        context.Score.Passed = false;
        Assert.That(Evaluate(context), Is.EqualTo((string[])["osu-plays-5000"]));
        context.Score.Processed = false;
        Assert.That(Evaluate(context), Is.Empty);
    }

    [Test]
    public void LovedMapsAllowIntroMedalsButDoNotAwardRankedSkillAndComboMedals()
    {
        AchievementEvaluationContext context = Context("HD");
        context.Beatmap.Status = BeatmapOnlineStatus.Loved;
        string[] medals = Evaluate(context);
        Assert.That(medals, Does.Contain("all-intro-hidden"));
        Assert.That(medals.Any(icon => icon.StartsWith("osu-skill-", StringComparison.Ordinal) || icon.StartsWith("osu-combo-", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void ComboAndPlayCountUseTheCurrentThresholdBandAndOwnedMedalsAreExcluded()
    {
        AchievementEvaluationContext context = Context();
        context.Score.MaxCombo = 750;
        context.Statistics!.PlayCount = 15000;
        Assert.That(Evaluate(context), Does.Contain("osu-combo-750"));
        Assert.That(Evaluate(context), Does.Not.Contain("osu-combo-500"));
        Assert.That(Evaluate(context), Does.Contain("osu-plays-15000"));
        Assert.That(AchievementEvaluator.Evaluate(context, AchievementEvaluator.Definitions.Select(item => item.Id).ToHashSet()), Is.Empty);
    }

    [Test]
    public void HighestTotalHitsThresholdUsesHitsRatherThanPlayCount()
    {
        AchievementEvaluationContext context = Context();
        context.Score.Mode = 1;
        context.Statistics!.TotalHits = 30000000;
        context.Statistics.PlayCount = 1;
        Assert.That(Evaluate(context), Does.Contain("taiko-hits-30000000"));
        context.Statistics.TotalHits = 0;
        context.Statistics.PlayCount = 30000000;
        Assert.That(Evaluate(context), Does.Not.Contain("taiko-hits-30000000"));
    }

    [Test]
    public void SecretMedalsUseLegacyDisplayScoreAndRecognizeNightcoreWithoutDoubleTime()
    {
        AchievementEvaluationContext context = Context("NC");
        context.Score.ClassicTotalScore = 777777;
        context.Score.TotalScore = 123456;
        context.Set!.Artist = "Nightcore";
        Assert.That(Evaluate(context), Does.Contain("all-secret-jackpot"));
        Assert.That(Evaluate(context), Does.Contain("all-secret-tothecore"));
        context.Score.ClassicTotalScore = 777778;
        Assert.That(Evaluate(context), Does.Not.Contain("all-secret-jackpot"));
    }

    [TestCase("nonstop", "", "", "", 522, 600)]
    [TestCase("prepared", "NF", "", "", 200, 230)]
    [TestCase("reckless", "HR SD", "", "", 200, 230)]
    [TestCase("uguushy", "HD NF", "", "", 200, 230)]
    [TestCase("hourbeforethedawn", "", "ginkiha", "EOS", 200, 230)]
    [TestCase("slowandsteady", "HT PF", "", "", 200, 230)]
    [TestCase("ntts", "NC", "", "", 30, 45)]
    [TestCase("realtor", "HR DT", "cYsmix", "House With Legs", 200, 230)]
    [TestCase("aeon", "FL HD HT", "", "", 200, 230)]
    [TestCase("deliberation", "HT", "", "", 200, 230)]
    public void PfcMedalsRequireTheFullComboRatherThanSs(string icon, string mods, string artist, string title, int drainTime, int totalLength)
    {
        AchievementEvaluationContext context = Context(mods.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        context.Set!.Artist = artist;
        context.Set.Title = title;
        context.Set.RankedDate = new DateTimeOffset(2011, 1, 1, 0, 0, 0, TimeSpan.Zero);
        context.Beatmap.HitLength = drainTime;
        context.Beatmap.TotalLength = totalLength;
        context.Difficulty.StarRating = 6.5;
        context.Score.MaxCombo = context.Difficulty.MaxCombo;
        context.Score.Rank = ScoreRank.S;
        context.Score.Accuracy = 0.98;
        Assert.That(Evaluate(context), Does.Contain("all-secret-" + icon));

        context.Score.Rank = ScoreRank.X;
        context.Score.Accuracy = 1;
        context.Score.MaxCombo--;
        Assert.That(Evaluate(context), Does.Not.Contain("all-secret-" + icon));
    }

    [Test]
    public void PfcMedalsRejectMissingModsAndUnknownMaximumCombo()
    {
        AchievementEvaluationContext context = Context("HD");
        context.Score.MaxCombo = context.Difficulty.MaxCombo;
        Assert.That(Evaluate(context), Does.Not.Contain("all-secret-uguushy"));

        context = Context("HD", "NF");
        context.Score.MaxCombo = context.Difficulty.MaxCombo = 0;
        context.Score.Rank = ScoreRank.XH;
        Assert.That(Evaluate(context), Does.Not.Contain("all-secret-uguushy"));
    }

    private static string[] Evaluate(AchievementEvaluationContext context)
        => AchievementEvaluator.Evaluate(context, new HashSet<int>()).Select(item => item.Icon).ToArray();

    private static AchievementEvaluationContext Context(params string[] mods) => new(
        new Score { Id = 42, UserId = 1, BeatmapId = 2, Processed = true, Passed = true, Mode = 0, MaxCombo = 800, Rank = ScoreRank.A, Accuracy = 0.98 },
        new Beatmap { Id = 2, BeatmapSetId = 10, Status = BeatmapOnlineStatus.Ranked, HitLength = 200, TotalLength = 230 },
        new BeatmapSet { Id = 10, Artist = "Artist", Title = "Song", Creator = "Mapper" },
        new UserStatistics { UserId = 1, PlayCount = 5000, TotalHits = 30000 },
        new DifficultyAttributes { StarRating = 5.5, MaxCombo = 2000 },
        mods.Select(acronym => new APIMod { Acronym = acronym }.ToMod(new OsuRuleset())).ToArray());
}