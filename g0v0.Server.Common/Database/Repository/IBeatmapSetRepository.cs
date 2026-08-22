// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for beatmap set records.
/// </summary>
public interface IBeatmapSetRepository
{
    /// <summary>
    /// Gets a beatmap set by its unique identifier.
    /// </summary>
    /// <param name="beatmapSetId">The beatmap set ID.</param>
    /// <returns>The matching beatmap set if found; otherwise, <see langword="null"/>.</returns>
    Task<BeatmapSet?> GetByIdAsync(int beatmapSetId);

    /// <summary>
    /// Gets a beatmap set by its unique identifier, including its beatmaps.
    /// </summary>
    /// <param name="beatmapSetId">The beatmap set ID.</param>
    /// <returns>The matching beatmap set if found; otherwise, <see langword="null"/>.</returns>
    Task<BeatmapSet?> GetByIdWithBeatmapsAsync(int beatmapSetId);

    /// <summary>
    /// Inserts or updates a beatmap set and its contained beatmaps in a single persistence operation.
    /// </summary>
    /// <param name="beatmapSet">The beatmap set to persist.</param>
    /// <returns>The persisted beatmap set.</returns>
    Task<BeatmapSet> UpsertWithBeatmapsAsync(BeatmapSet beatmapSet);

    /// <summary>
    /// Removes a beatmap set record.
    /// </summary>
    /// <param name="beatmapSet">The beatmap set to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(BeatmapSet beatmapSet);
}