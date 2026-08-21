// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for beatmap records.
/// </summary>
public interface IBeatmapRepository
{
    /// <summary>
    /// Gets a beatmap by its unique identifier.
    /// </summary>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <returns>The matching beatmap if found; otherwise, <see langword="null"/>.</returns>
    Task<Beatmap?> GetByIdAsync(int beatmapId);

    /// <summary>
    /// Gets a beatmap by its checksum.
    /// </summary>
    /// <param name="checksum">The beatmap checksum.</param>
    /// <returns>The matching beatmap if found; otherwise, <see langword="null"/>.</returns>
    Task<Beatmap?> GetByChecksumAsync(string checksum);

    /// <summary>
    /// Gets all beatmaps in a beatmap set.
    /// </summary>
    /// <param name="beatmapSetId">The beatmap set ID.</param>
    /// <returns>A list of beatmaps belonging to the specified beatmap set.</returns>
    Task<IReadOnlyList<Beatmap>> GetByBeatmapSetIdAsync(long beatmapSetId);

    /// <summary>
    /// Gets all beatmaps mapped by a specific user.
    /// </summary>
    /// <param name="mapperId">The mapper user ID.</param>
    /// <returns>A list of beatmaps created by the specified mapper.</returns>
    Task<IReadOnlyList<Beatmap>> GetByMapperIdAsync(int mapperId);

    /// <summary>
    /// Creates a new beatmap record.
    /// </summary>
    /// <param name="beatmap">The beatmap to create.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateAsync(Beatmap beatmap);

    /// <summary>
    /// Updates an existing beatmap record.
    /// </summary>
    /// <param name="beatmap">The beatmap to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateAsync(Beatmap beatmap);

    /// <summary>
    /// Removes a beatmap record.
    /// </summary>
    /// <param name="beatmap">The beatmap to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(Beatmap beatmap);
}