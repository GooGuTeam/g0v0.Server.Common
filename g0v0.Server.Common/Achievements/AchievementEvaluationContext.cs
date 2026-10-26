// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;

namespace g0v0.Server.Common.Achievements;

/// <summary>The authoritative inputs used to evaluate score-based medals.</summary>
/// <param name="Score">The processed score.</param>
/// <param name="Beatmap">The beatmap metadata.</param>
/// <param name="Set">Optional set metadata used by song-specific medals.</param>
/// <param name="Statistics">The user's statistics after processing this score.</param>
/// <param name="Difficulty">Difficulty calculated with the score's mods.</param>
/// <param name="Mods">The effective mods, excluding the legacy compatibility Classic mod.</param>
public sealed record AchievementEvaluationContext(Score Score, Beatmap Beatmap, BeatmapSet? Set, UserStatistics? Statistics, DifficultyAttributes Difficulty, IReadOnlyList<Mod> Mods);