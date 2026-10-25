// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Fetching;
using osu.Game.Beatmaps;
using Beatmap = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Shares favourite and rating behavior between client APIs.</summary>
/// <param name="feedback">The feedback repository.</param>
/// <param name="beatmaps">The beatmap repository.</param>
/// <param name="sets">The beatmap set repository.</param>
/// <param name="fetcher">The metadata fetcher.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class BeatmapFeedbackService(
    IBeatmapFeedbackRepository feedback,
    IBeatmapRepository beatmaps,
    IBeatmapSetRepository sets,
    IFetcher fetcher,
    TimeProvider timeProvider)
{
    /// <summary>Adds an existing or fetched set to a user's favourites.</summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="beatmapSetId">The set ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True for a new favourite, false for a duplicate.</returns>
    public async Task<bool> AddFavouriteAsync(int userId, int beatmapSetId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(beatmapSetId);
        await sets.GetOrFetchByIdAsync(beatmapSetId, fetcher, cancellationToken).ConfigureAwait(false);
        FavouriteBeatmapSet favourite = new()
        {
            UserId = userId,
            BeatmapSetId = beatmapSetId,
            Date = timeProvider.GetUtcNow(),
        };
        return await feedback.AddFavouriteAsync(favourite, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Checks eligibility or records a vote for the set containing the requested beatmap.</summary>
    /// <param name="userId">The voting user ID.</param>
    /// <param name="checksum">The beatmap MD5.</param>
    /// <param name="rating">The vote, or null to check eligibility.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rating outcome and current average.</returns>
    public async Task<BeatmapRatingResult> RateAsync(int userId, string checksum, int? rating, CancellationToken cancellationToken = default)
    {
        if (rating is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(rating.Value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(rating.Value, 10);
        }

        Beatmap? beatmap = await beatmaps.GetByChecksumAsync(checksum).ConfigureAwait(false);
        if (beatmap is null)
        {
            return new BeatmapRatingResult(BeatmapRatingStatus.NotFound);
        }

        if (beatmap.Status < BeatmapOnlineStatus.Ranked)
        {
            return new BeatmapRatingResult(BeatmapRatingStatus.NotRanked);
        }

        if (rating is null && !await feedback.HasRatingAsync(userId, beatmap.BeatmapSetId, cancellationToken).ConfigureAwait(false))
        {
            return new BeatmapRatingResult(BeatmapRatingStatus.CanRate);
        }

        if (rating is not null)
        {
            BeatmapRating vote = new()
            {
                UserId = userId,
                BeatmapSetId = beatmap.BeatmapSetId,
                Rating = rating.Value,
            };
            await feedback.AddRatingAsync(vote, cancellationToken).ConfigureAwait(false);
        }

        double average = await feedback.GetAverageRatingAsync(beatmap.BeatmapSetId, cancellationToken).ConfigureAwait(false);
        return new BeatmapRatingResult(BeatmapRatingStatus.AlreadyVoted, average);
    }
}