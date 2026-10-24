// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using Score = g0v0.Server.Common.Database.Models.Score;

namespace g0v0.Server.Common.Utils;

/// <summary>
/// Applies a completed solo score to personal statistics.
/// </summary>
public static class UserScoreStatisticsUtils
{
    private static readonly long[] LevelRequirements =
    [
        30000, 100000, 210000, 360000, 550000, 780000, 1050000, 1360000, 1710000, 2100000, 2530000, 3000000, 3510000, 4060000, 4650000, 5280000, 5950000, 6660000, 7410000, 8200000, 9030000, 9900000, 10810000, 11760000, 12750000, 13780000, 14850000, 15960000, 17110000, 18300000, 19530000, 20800000, 22110000, 23460000, 24850000, 26280000, 27750000, 29260000, 30810000, 32400000, 34030000, 35700000, 37410000, 39160000, 40950000, 42780000, 44650000, 46560000, 48510000, 50500000, 52530000, 54600000, 56710000, 58860000, 61050000, 63280000, 65550000, 67860000, 70210001, 72600001, 75030002, 77500003, 80010006, 82560010, 85150019, 87780034, 90450061, 93160110, 95910198, 98700357, 101530643, 104401157, 107312082, 110263748, 113256747, 116292144, 119371859, 122499346, 125680824, 128927482, 132259468, 135713043, 139353477, 143298259, 147758866, 153115959, 160054726, 169808506, 184597311, 208417160, 248460887, 317675597, 439366075, 655480935, 1041527682, 1733419828, 2975801691, 5209033044,
        9225761479, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999, 99999999999
    ];

    /// <summary>
    /// Updates lifetime counters and best-per-beatmap aggregates. Call once within the score completion transaction.
    /// </summary>
    /// <param name="statistics">
    /// The account statistics for this exact stored mode.
    /// </param>
    /// <param name="score">
    /// The completed score.
    /// </param>
    /// <param name="history">
    /// Previously processed scores for the same account and mode.
    /// </param>
    /// <param name="playTime">
    /// Actual play duration in seconds, bounded by the modded .osu length.
    /// </param>
    public static void Apply(UserStatistics statistics, Score score, IEnumerable<Score> history, int playTime)
    {
        statistics.TotalScore += score.TotalScore;
        statistics.Count300 += Count(HitResult.Great) + Count(HitResult.Perfect) + Count(HitResult.LargeBonus);
        statistics.Count100 += Count(HitResult.Ok) + Count(HitResult.Good);
        statistics.Count50 += Count(HitResult.Meh);
        statistics.CountMiss += Count(HitResult.Miss);
        statistics.TotalHits += score.Statistics.Where(s => s.Key is HitResult.Great or HitResult.Perfect or HitResult.Ok or HitResult.Good or HitResult.Meh
            or HitResult.LargeTickHit or HitResult.LargeTickMiss or HitResult.SmallTickHit or HitResult.LargeBonus).Sum(s => (long)s.Value);
        if (ScoreModeUtils.GetBaseRulesetId(score.Mode) == 2)
        {
            statistics.Count100 += Count(HitResult.LargeTickHit);
            statistics.Count50 += Count(HitResult.SmallTickHit);
            statistics.CountMiss += Count(HitResult.LargeTickMiss);
        }

        if (IsValidPlay(score, playTime))
        {
            statistics.PlayCount++;
            statistics.PlayTime += playTime;
        }

        // The two aggregates mirror the materialised leaderboard and best-score tables: every passed
        // ranked score joins the ranked-score total, while only a score that was awarded performance
        // points joins the personal best. A score whose mods disqualify performance carries zero pp,
        // so the second filter needs nothing beyond the stored value.
        Score[] eligible = history.Append(score)
            .Where(s => s is { Passed: true, Ranked: true } && s.UserId == score.UserId && s.Mode == score.Mode)
            .ToArray();
        Score[] scoreBests = eligible.GroupBy(s => s.BeatmapId)
            .Select(g => g.OrderByDescending(s => s.TotalScore).ThenBy(s => s.EndedAt).ThenBy(s => s.Id).First()).ToArray();
        Score[] ppBests = eligible.Where(s => s.Pp > 0).GroupBy(s => s.BeatmapId)
            .Select(g => g.OrderByDescending(s => s.Pp).ThenBy(s => s.EndedAt).ThenBy(s => s.Id).First())
            .OrderByDescending(s => s.Pp).Take(1000).ToArray();
        statistics.RankedScore = scoreBests.Sum(s => (long)s.TotalScore);
        statistics.MaximumCombo = Math.Max(statistics.MaximumCombo, scoreBests.Select(s => s.MaxCombo).DefaultIfEmpty(0).Max());
        statistics.GradeSs = scoreBests.Count(s => s.Rank == ScoreRank.X);
        statistics.GradeSsh = scoreBests.Count(s => s.Rank == ScoreRank.XH);
        statistics.GradeS = scoreBests.Count(s => s.Rank == ScoreRank.S);
        statistics.GradeSh = scoreBests.Count(s => s.Rank == ScoreRank.SH);
        statistics.GradeA = scoreBests.Count(s => s.Rank == ScoreRank.A);
        statistics.Pp = ppBests.Select((s, i) => s.Pp * Math.Pow(0.95, i)).Sum();
        double weightedAccuracy = ppBests.Select((s, i) => s.Accuracy * Math.Pow(0.95, i)).Sum();
        statistics.HitAccuracy = ppBests.Length == 0 ? 0 : Math.Clamp(weightedAccuracy * 100 / (20 * (1 - Math.Pow(0.95, ppBests.Length))), 0, 100);
        statistics.LevelCurrent = GetLevel(statistics.TotalScore);
        return;

        int Count(HitResult result) => score.Statistics.TryGetValue(result, out int count) ? count : 0;
    }

    /// <summary>
    /// Decides whether a completed score counts as a play.
    /// </summary>
    /// <param name="score">
    /// The completed score.
    /// </param>
    /// <param name="playTime">
    /// Actual play duration in seconds, bounded by the modded .osu length.
    /// </param>
    /// <returns>
    /// Whether the score increments the play counters.
    /// </returns>
    /// <remarks>
    /// A failed score still counts when the player reached a meaningful amount of the map: more than
    /// eight seconds, at least 5000 points and at least a tenth of the maximum hits (capped at 20).
    /// The same predicate gates the per-beatmap and monthly play counts, so all three counters agree.
    /// </remarks>
    public static bool IsValidPlay(Score score, int playTime)
    {
        ArgumentNullException.ThrowIfNull(score);
        int maximumHits = score.MaximumStatistics.Values.Sum();
        int successfulHits = score.Statistics.Where(s => s.Key is not (HitResult.Miss or HitResult.SmallTickMiss or HitResult.IgnoreMiss)).Sum(s => s.Value);
        return score.Passed || (playTime > 8 && score.TotalScore >= 5000 && successfulHits >= Math.Min(0.1 * maximumHits, 20));
    }

    private static double GetLevel(long totalScore)
    {
        // Same level increments as osu_lazer_api's calculate_score_to_level.
        long[] requirements = LevelRequirements;
        double level = 0;
        double remaining = Math.Max(0, totalScore);
        for (int i = 0; i < requirements.Length - 1 && remaining > 0; i++)
        {
            level += Math.Min(1, remaining / requirements[i]);
            remaining -= requirements[i];
        }

        return level + (Math.Max(0, remaining) / requirements[^1]) + 1;
    }
}