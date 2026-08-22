// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Fetching;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>
/// MySQL-backed implementation of <see cref="IBeatmapSetRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class BeatmapSetRepository(MysqlDbContext context) : IBeatmapSetRepository, IMySqlRepository
{
    /// <inheritdoc/>
    public async Task<BeatmapSet?> GetByIdAsync(int beatmapSetId)
    {
        return await context.BeatmapSets
            .FirstOrDefaultAsync(b => b.Id == beatmapSetId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<BeatmapSet?> GetByIdWithBeatmapsAsync(int beatmapSetId)
    {
        return await context.BeatmapSets
            .Include(b => b.Beatmaps)
            .FirstOrDefaultAsync(b => b.Id == beatmapSetId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<BeatmapSet> GetOrFetchByIdAsync(int beatmapSetId, IFetcher fetcher, CancellationToken cancellationToken = default)
    {
        BeatmapSet? beatmapSet = await GetByIdWithBeatmapsAsync(beatmapSetId).ConfigureAwait(false);
        return beatmapSet ?? await fetcher.FetchBeatmapSetAsync(beatmapSetId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<BeatmapSet> UpsertWithBeatmapsAsync(BeatmapSet beatmapSet)
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync().ConfigureAwait(false)
            : null;

        try
        {
            BeatmapSet? existing = await context.BeatmapSets
                .Include(b => b.Beatmaps)
                .FirstOrDefaultAsync(b => b.Id == beatmapSet.Id)
                .ConfigureAwait(false);

            if (existing == null)
            {
                await context.BeatmapSets.AddAsync(beatmapSet).ConfigureAwait(false);
            }
            else
            {
                context.Entry(existing).CurrentValues.SetValues(beatmapSet);

                Dictionary<int, Beatmap> existingBeatmaps = existing.Beatmaps.ToDictionary(b => b.Id);
                foreach (Beatmap beatmap in beatmapSet.Beatmaps)
                {
                    if (existingBeatmaps.TryGetValue(beatmap.Id, out Beatmap? existingBeatmap))
                    {
                        context.Entry(existingBeatmap).CurrentValues.SetValues(beatmap);
                    }
                    else
                    {
                        context.Beatmaps.Add(beatmap);
                    }
                }
            }

            await context.SaveChangesAsync().ConfigureAwait(false);

            if (transaction != null)
            {
                await transaction.CommitAsync().ConfigureAwait(false);
            }

            return existing ?? beatmapSet;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(BeatmapSet beatmapSet)
    {
        context.BeatmapSets.Remove(beatmapSet);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}