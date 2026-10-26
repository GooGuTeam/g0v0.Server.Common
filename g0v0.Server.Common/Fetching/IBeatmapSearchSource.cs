// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Beatmaps;

namespace g0v0.Server.Common.Fetching;

/// <summary>Retrieves an uncached page using the Fetcher's authenticated osu! API connection.</summary>
public interface IBeatmapSearchSource
{
    /// <summary>Fetches detached search metadata without overwriting complete stored beatmap records.</summary>
    /// <param name="query">The public search filters.</param>
    /// <param name="cursor">The next-page cursor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The upstream search page.</returns>
    Task<BeatmapSearchResult> FetchSearchAsync(BeatmapSearchQuery query, string? cursor, CancellationToken cancellationToken = default);
}