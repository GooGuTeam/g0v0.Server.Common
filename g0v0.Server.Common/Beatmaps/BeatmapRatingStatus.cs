// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Describes the outcome of a beatmap rating request.</summary>
public enum BeatmapRatingStatus
{
    /// <summary>The requested beatmap does not exist.</summary>
    NotFound,

    /// <summary>The beatmap is not eligible for ratings.</summary>
    NotRanked,

    /// <summary>The user has not voted yet.</summary>
    CanRate,

    /// <summary>The user's vote has already been recorded.</summary>
    AlreadyVoted,
}