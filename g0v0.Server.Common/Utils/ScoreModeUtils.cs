// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using osu.Game.Online.API;
using osu.Game.Rulesets.Mods;

namespace g0v0.Server.Common.Utils;

/// <summary>
/// Maps stored RX/AP mode IDs to their underlying gameplay rulesets.
/// </summary>
public static class ScoreModeUtils
{
    /// <summary>
    /// Resolves the gameplay ruleset for a stored score mode.
    /// </summary>
    /// <param name="mode">
    /// The persisted mode ID.
    /// </param>
    /// <returns>
    /// The underlying ruleset ID.
    /// </returns>
    public static int GetBaseRulesetId(int mode) => mode switch { 4 or 5 => 0, 6 => 1, 7 => 2, _ => mode };

    /// <summary>
    /// Selects a separate RX/AP mode only when its feature is enabled.
    /// </summary>
    /// <param name="mode">
    /// The base ruleset ID.
    /// </param>
    /// <param name="mods">
    /// The submitted mods.
    /// </param>
    /// <param name="config">
    /// The gameplay feature flags.
    /// </param>
    /// <returns>
    /// The mode to persist on scores and personal statistics.
    /// </returns>
    public static int GetScoreMode(int mode, IEnumerable<APIMod> mods, GameConfig config)
    {
        string[] acronyms = mods.Select(m => m.Acronym).ToArray();
        bool autopilot = config.EnableAutopilot && acronyms.Contains("AP", StringComparer.Ordinal);
        bool relax = config.EnableRelax && acronyms.Contains("RX", StringComparer.Ordinal);
        return (mode, autopilot, relax) switch
        {
            (0, true, _) => 5,
            (0, _, true) => 4,
            (1, _, true) => 6,
            (2, _, true) => 7,
            _ => mode
        };
    }

    /// <summary>
    /// Decides whether the mods of a score still allow it to award performance.
    /// </summary>
    /// <param name="mode">
    /// The stored score mode, which tells RX and AP apart from the vanilla rulesets.
    /// </param>
    /// <param name="mods">
    /// The mods the score was played with, as the ruleset instantiated them.
    /// </param>
    /// <param name="config">
    /// The gameplay feature flags.
    /// </param>
    /// <param name="legacyScore">
    /// Whether the score is a legacy score, which is scored through <see cref="ModClassic"/>.
    /// </param>
    /// <returns>
    /// Whether the score may contribute performance.
    /// </returns>
    /// <remarks>
    /// osu! marks every mod that osu-web accepts on a ranked leaderboard with
    /// <see cref="Mod.Ranked"/>, which covers the mods that disqualify performance (<c>AT</c>,
    /// <c>CN</c>), the mods that are only ranked with their default settings (<c>HD</c>, <c>DT</c>)
    /// and the conversion mods, in one place.
    /// </remarks>
    public static bool CanAwardPerformance(int mode, IEnumerable<Mod> mods, GameConfig config, bool legacyScore)
    {
        ArgumentNullException.ThrowIfNull(mods);
        ArgumentNullException.ThrowIfNull(config);
        return mods.All(mod => IsRanked(mode, mod, config, legacyScore));
    }

    private static bool IsRanked(int mode, Mod mod, GameConfig config, bool legacyScore)
    {
        if (mod.Ranked)
        {
            return true;
        }

        // The stable submission injects the classic mod so the legacy encoder scores the play, and
        // osu! then treats the result as a score set on osu-stable; the mod itself is unranked.
        if (legacyScore && mod is ModClassic)
        {
            return true;
        }

        // Relax and autopilot are unranked mods that these two features re-enable when the server is
        // configured to offer them as their own modes.
        return mod.Acronym switch
        {
            "RX" => config.EnableRelax && mode is 4 or 6 or 7,
            "AP" => config.EnableAutopilot && mode == 5,
            _ => false
        };
    }
}