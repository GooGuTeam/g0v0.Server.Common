// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Text;
using g0v0.Server.Common.Rulesets;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Scoring;

namespace g0v0.Server.Common.Utils;

/// <summary>
/// Calculates difficulty and performance using the installed osu! rulesets.
/// </summary>
/// <param name="rulesets">The registered rulesets.</param>
public class ScoreAttributeUtils(RulesetManager rulesets)
{
    /// <summary>
    /// Decodes an osu! file without requiring a filesystem or game database.
    /// </summary>
    /// <param name="content">The complete .osu file.</param>
    /// <returns>A working beatmap suitable for conversion and calculation.</returns>
    public static FlatWorkingBeatmap DecodeBeatmap(string content)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(content));
        using LineBufferedReader reader = new(stream);
        return new FlatWorkingBeatmap(osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader));
    }

    /// <summary>
    /// Calculates attributes for a ruleset and mod combination, including converted maps.
    /// </summary>
    /// <param name="beatmap">The source beatmap.</param>
    /// <param name="mode">The target ruleset ID.</param>
    /// <param name="mods">The selected mods.</param>
    /// <param name="cancellationToken">Calculation cancellation.</param>
    /// <returns>The ruleset-specific difficulty attributes.</returns>
    public DifficultyAttributes CalculateDifficultyAttributes(IWorkingBeatmap beatmap, int mode, IEnumerable<APIMod> mods, CancellationToken cancellationToken = default)
    {
        Ruleset ruleset = rulesets.GetRuleset(ScoreModeUtils.GetBaseRulesetId(mode));
        return ruleset.CreateDifficultyCalculator(beatmap).Calculate(mods.Select(m => m.ToMod(ruleset)), cancellationToken);
    }

    /// <summary>
    /// Calculates performance from a score and matching difficulty attributes.
    /// </summary>
    /// <param name="score">The score, including statistics and actual mods.</param>
    /// <param name="difficulty">Attributes calculated for the same map, mode and mods.</param>
    /// <returns>The ruleset-specific performance attributes.</returns>
    public PerformanceAttributes CalculatePerformanceAttributes(ScoreInfo score, DifficultyAttributes difficulty)
    {
        return rulesets.GetRuleset(score.Ruleset.OnlineID).CreatePerformanceCalculator()?.Calculate(score, difficulty)
            ?? throw new NotSupportedException("The ruleset does not provide performance calculation.");
    }

    /// <summary>
    /// Gets the stable score multiplier, including conversion and ScoreV2 rules.
    /// </summary>
    /// <param name="beatmap">The original, unmodified beatmap.</param>
    /// <param name="mode">The target legacy ruleset ID.</param>
    /// <param name="mods">The selected mods.</param>
    /// <returns>The stable multiplier; zero means the original score cannot be recovered by division.</returns>
    public double GetLegacyScoreMultiplier(IBeatmap beatmap, int mode, IReadOnlyList<Mod> mods)
    {
        return ((ILegacyRuleset)rulesets.GetRuleset(ScoreModeUtils.GetBaseRulesetId(mode))).CreateLegacyScoreSimulator()
            .GetLegacyScoreMultiplier(mods, LegacyBeatmapConversionDifficultyInfo.FromBeatmap(beatmap));
    }
}