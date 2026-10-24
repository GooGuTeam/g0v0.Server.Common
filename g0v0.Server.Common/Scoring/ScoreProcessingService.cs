// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Fetching;
using g0v0.Server.Common.Rulesets;
using g0v0.Server.Common.Utils;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using Score = g0v0.Server.Common.Database.Models.Score;

namespace g0v0.Server.Common.Scoring;

/// <summary>
/// Calculates a persisted solo score and personal statistics using its original .osu file.
/// </summary>
/// <param name="fetcher">
/// The beatmap file fetcher.
/// </param>
/// <param name="beatmaps">
/// The beatmap metadata repository.
/// </param>
/// <param name="scores">
/// The score repository.
/// </param>
/// <param name="rulesets">
/// The registered rulesets.
/// </param>
/// <param name="attributes">
/// The shared difficulty and performance utility.
/// </param>
/// <param name="configs">
/// Reads the gameplay feature flags that decide whether RX and AP award performance.
/// </param>
public sealed class ScoreProcessingService(IFetcher fetcher, IBeatmapRepository beatmaps, IScoreRepository scores, RulesetManager rulesets, ScoreAttributeUtils attributes, ConfigManager configs) : IScoreProcessingService
{
    private readonly GameConfig _game = configs.Get<GameConfig>();

    /// <summary>
    /// Processes a queued score, marking it complete only after all calculations succeed.
    /// </summary>
    /// <param name="score">
    /// The unprocessed record.
    /// </param>
    /// <param name="cancellationToken">
    /// Worker cancellation.
    /// </param>
    /// <returns>
    /// A task representing calculation and persistence.
    /// </returns>
    public async Task ProcessAsync(Score score, CancellationToken cancellationToken)
    {
        string content = await fetcher.FetchBeatmapRawAsync(score.BeatmapId, cancellationToken);

        // HttpContent.ReadAsStringAsync strips a UTF-8 BOM; accept that exact byte-preserving variant as well.
#pragma warning disable CA5351 // The osu! wire protocol identifies beatmaps with MD5.
        if (!string.Equals(Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(content))), score.BeatmapChecksum, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("\uFEFF" + content))), score.BeatmapChecksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The fetched .osu does not match the submitted beatmap checksum.");
        }

#pragma warning restore CA5351

        FlatWorkingBeatmap working = ScoreAttributeUtils.DecodeBeatmap(content);
        int baseMode = ScoreModeUtils.GetBaseRulesetId(score.Mode);
        Ruleset ruleset = rulesets.GetRuleset(baseMode);
        Mod[] mods = score.Mods.Select(m => m.ToMod(ruleset)).ToArray();
        bool isLegacyScore = score.ClassicTotalScore != 0;
        ScoreInfo info = new()
        {
            Ruleset = ruleset.RulesetInfo,
            BeatmapInfo = working.BeatmapInfo,
            Mods = mods,
            IsLegacyScore = isLegacyScore,
            LegacyTotalScore = isLegacyScore ? score.ClassicTotalScore : null,
            TotalScore = isLegacyScore ? score.ClassicTotalScore : score.TotalScore,
            TotalScoreWithoutMods = score.TotalScoreWithoutMods,
            TotalScoreVersion = 0,
            Statistics = new Dictionary<HitResult, int>(score.Statistics),
            MaxCombo = score.MaxCombo,
            Passed = score.Passed
        };

        DifficultyAttributes difficulty = attributes.CalculateDifficultyAttributes(working, baseMode, score.Mods, cancellationToken);
        IBeatmap playable = working.GetPlayableBeatmap(ruleset.RulesetInfo, mods, cancellationToken);
        using ScoreProcessor processor = ruleset.CreateScoreProcessor();
        processor.Mods.Value = mods;
        processor.ApplyBeatmap(playable);
        if (isLegacyScore)
        {
            LegacyScoreDecoder.PopulateMaximumStatistics(info, working);
        }
        else
        {
            info.MaximumStatistics = score.MaximumStatistics.Count > 0
                ? new Dictionary<HitResult, int>(score.MaximumStatistics) : processor.MaximumStatistics;
        }

        if (isLegacyScore)
        {
            // Keep the submitted stable score in ClassicTotalScore and persist the converted lazer total below.
            StandardisedScoreMigrationTools.UpdateFromLegacy(info, working);
            info.TotalScoreVersion = LegacyScoreEncoder.LATEST_VERSION;
        }
        else
        {
            StandardisedScoreMigrationTools.UpdateToLatestScoring(info, working);
        }

        if (isLegacyScore)
        {
            double multiplier = attributes.GetLegacyScoreMultiplier(working.Beatmap, baseMode, mods);
            score.ClassicTotalScoreWithoutMods = multiplier > 0 ? checked((long)Math.Round(score.ClassicTotalScore / multiplier)) : 0;
        }

        score.TotalScore = checked((int)info.TotalScore);
        score.TotalScoreWithoutMods = checked((int)info.TotalScoreWithoutMods);
        score.Accuracy = info.Accuracy;
        score.Rank = score.Passed ? info.Rank : ScoreRank.F;

        score.MaximumStatistics = new Dictionary<HitResult, int>(processor.MaximumStatistics);
        g0v0.Server.Common.Database.Models.Beatmap beatmap = await beatmaps.GetByIdAsync(score.BeatmapId)
            ?? throw new InvalidDataException("Queued score beatmap metadata is missing.");
        score.Ranked = beatmap.Status is BeatmapOnlineStatus.Ranked or BeatmapOnlineStatus.Approved;
        score.Pp = score.Passed && ScoreModeUtils.CanAwardPerformance(score.Mode, mods, _game, isLegacyScore)
            ? attributes.CalculatePerformanceAttributes(info, difficulty).Total : 0;
        if (!double.IsFinite(score.Pp) || !double.IsFinite(score.Accuracy))
        {
            throw new InvalidDataException("Score calculation returned non-finite values.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        double length = playable.CalculatePlayableBounds().end / 1000 / osu.Game.Utils.ModUtils.CalculateRateWithMods(mods);
        int playTime = (int)Math.Max(0, Math.Min(length, (score.EndedAt - score.StartedAt).TotalSeconds));
        await scores.CompleteProcessingAsync(score, playTime);
    }
}