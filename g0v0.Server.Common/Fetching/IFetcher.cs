// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Fetching;

/// <summary>
/// Fetches osu! beatmap metadata and raw beatmap files from the osu! API.
/// </summary>
public interface IFetcher
{
    /// <summary>
    /// Fetches and persists a beatmap by its ID, along with its beatmap set and sibling beatmaps.
    /// </summary>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted beatmap.</returns>
    Task<Beatmap> FetchBeatmapAsync(int beatmapId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches and persists a beatmap by its checksum, along with its beatmap set and sibling beatmaps.
    /// </summary>
    /// <param name="checksum">The beatmap checksum.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted beatmap.</returns>
    Task<Beatmap> FetchBeatmapAsync(string checksum, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches and persists a beatmap set by its ID, along with all of its beatmaps.
    /// </summary>
    /// <param name="beatmapSetId">The beatmap set ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted beatmap set.</returns>
    Task<BeatmapSet> FetchBeatmapSetAsync(int beatmapSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the raw .osu file content for a beatmap, with Redis caching and mirror fallback.
    /// </summary>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw beatmap file content.</returns>
    Task<string> FetchBeatmapRawAsync(int beatmapId, CancellationToken cancellationToken = default);
}