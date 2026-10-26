// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Shares cached beatmapset search and metadata lookup between client APIs.</summary>
public interface IBeatmapSearchService
{
    /// <summary>Searches the public catalogue using a cached upstream cursor page.</summary>
    /// <param name="query">The search filters.</param>
    /// <param name="cursor">The continuation cursor, or null for the first page.</param>
    /// <param name="cancellationToken">Cancels this caller's wait.</param>
    /// <returns>The matching sets and next cursor.</returns>
    Task<BeatmapSearchResult> SearchAsync(BeatmapSearchQuery query, string? cursor = null, CancellationToken cancellationToken = default);

    /// <summary>Looks up a set from shared storage, fetching missing metadata when needed.</summary>
    /// <param name="beatmapSetId">The preferred set ID.</param>
    /// <param name="beatmapId">A difficulty ID, used when no set ID is supplied.</param>
    /// <param name="checksum">A difficulty checksum, used when neither ID is supplied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The set, or null when it does not exist.</returns>
    Task<BeatmapSet?> LookupAsync(int? beatmapSetId = null, int? beatmapId = null, string? checksum = null, CancellationToken cancellationToken = default);
}