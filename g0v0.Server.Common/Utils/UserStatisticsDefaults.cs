// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;

namespace g0v0.Server.Common.Utils;

/// <summary>
/// Decides which per-mode statistics rows a freshly registered account starts with.
/// </summary>
public static class UserStatisticsDefaults
{
    /// <summary>
    /// Lists the mode IDs a new account keeps statistics for.
    /// </summary>
    /// <param name="config">The gameplay feature flags that decide the available modes.</param>
    /// <returns>The mode IDs, ordered ascending.</returns>
    public static IReadOnlyList<int> GetModes(GameConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        List<int> modes = [0, 1, 2, 3];
        if (config.EnableRelax)
        {
            // Relax owns osu!, taiko and catch; mania has no relax ruleset.
            modes.AddRange([4, 6, 7]);
        }

        if (config.EnableAutopilot)
        {
            modes.Add(5);
        }

        modes.Sort();
        return modes;
    }
}