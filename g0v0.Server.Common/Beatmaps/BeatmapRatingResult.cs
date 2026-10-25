// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Describes rating eligibility and the current set-wide average.</summary>
/// <param name="Status">The outcome.</param>
/// <param name="AverageRating">The current average when a vote exists.</param>
public sealed record BeatmapRatingResult(BeatmapRatingStatus Status, double AverageRating = 0);