// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// Stores gameplay feature flags that affect score and ruleset behavior.
/// </summary>
[ConfigFile("game")]
public class GameConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether relax rulesets are enabled.
    /// </summary>
    [Reloadable]
    public bool EnableRelax { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether autopilot rulesets are enabled.
    /// </summary>
    [Reloadable]
    public bool EnableAutopilot { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether every beatmap can have a leaderboard.
    /// </summary>
    [Reloadable]
    public bool EnableAllBeatmapLeaderboard { get; set; }
}