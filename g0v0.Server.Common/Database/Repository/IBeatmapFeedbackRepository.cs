// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>Persists favourites and set-wide ratings .</summary>
public interface IBeatmapFeedbackRepository
{
    /// <summary>Lists the user's favourite set IDs in ascending order.</summary>
    /// <param name="userId">The owner ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The favourite set IDs.</returns>
    Task<IReadOnlyList<int>> GetFavouritesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a favourite unless it already exists.</summary>
    /// <param name="favourite">The favourite to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether a new favourite was added.</returns>
    Task<bool> AddFavouriteAsync(FavouriteBeatmapSet favourite, CancellationToken cancellationToken = default);

    /// <summary>Checks whether the user has rated this set.</summary>
    /// <param name="userId">The voting user ID.</param>
    /// <param name="beatmapSetId">The set ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether a rating exists.</returns>
    Task<bool> HasRatingAsync(int userId, int beatmapSetId, CancellationToken cancellationToken = default);

    /// <summary>Records a rating once; retries never replace an existing vote.</summary>
    /// <param name="rating">The vote to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether a new vote was recorded.</returns>
    Task<bool> AddRatingAsync(BeatmapRating rating, CancellationToken cancellationToken = default);

    /// <summary>Gets the average of all votes for a set.</summary>
    /// <param name="beatmapSetId">The set ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The average rating, or zero when there are no votes.</returns>
    Task<double> GetAverageRatingAsync(int beatmapSetId, CancellationToken cancellationToken = default);
}