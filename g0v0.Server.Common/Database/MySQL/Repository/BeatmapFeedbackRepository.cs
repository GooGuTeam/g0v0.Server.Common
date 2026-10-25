// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>Persists beatmap feedback using MySQL.</summary>
/// <param name="context">The database context.</param>
public class BeatmapFeedbackRepository(MysqlDbContext context) : IBeatmapFeedbackRepository, IMySqlRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<int>> GetFavouritesAsync(int userId, CancellationToken cancellationToken = default)
        => await context.FavouriteBeatmapSets.Where(favourite => favourite.UserId == userId)
            .OrderBy(favourite => favourite.BeatmapSetId).Select(favourite => favourite.BeatmapSetId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<bool> AddFavouriteAsync(FavouriteBeatmapSet favourite, CancellationToken cancellationToken = default)
    {
        if (await context.FavouriteBeatmapSets.AnyAsync(
                item => item.UserId == favourite.UserId && item.BeatmapSetId == favourite.BeatmapSetId, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        context.FavouriteBeatmapSets.Add(favourite);
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            context.Entry(favourite).State = EntityState.Detached;
            if (await context.FavouriteBeatmapSets.AnyAsync(
                    item => item.UserId == favourite.UserId && item.BeatmapSetId == favourite.BeatmapSetId, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> HasRatingAsync(int userId, int beatmapSetId, CancellationToken cancellationToken = default)
        => context.BeatmapRatings.AnyAsync(rating => rating.UserId == userId && rating.BeatmapSetId == beatmapSetId, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> AddRatingAsync(BeatmapRating rating, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rating.Rating, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rating.Rating, 10);
        if (await HasRatingAsync(rating.UserId, rating.BeatmapSetId, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        context.BeatmapRatings.Add(rating);
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            context.Entry(rating).State = EntityState.Detached;
            if (await HasRatingAsync(rating.UserId, rating.BeatmapSetId, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<double> GetAverageRatingAsync(int beatmapSetId, CancellationToken cancellationToken = default)
        => await context.BeatmapRatings.Where(rating => rating.BeatmapSetId == beatmapSetId)
            .Select(rating => (double?)rating.Rating).AverageAsync(cancellationToken).ConfigureAwait(false) ?? 0;
}