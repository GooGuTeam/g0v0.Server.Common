// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Fetching;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>
/// MySQL-backed implementation of <see cref="IBeatmapRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class BeatmapRepository(MysqlDbContext context) : IBeatmapRepository, IMySqlRepository
{
    /// <inheritdoc/>
    public async Task<Beatmap?> GetByIdAsync(int beatmapId)
    {
        return await context.Beatmaps
            .FirstOrDefaultAsync(b => b.Id == beatmapId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Beatmap?> GetByChecksumAsync(string checksum)
    {
        return await context.Beatmaps
            .FirstOrDefaultAsync(b => b.Checksum == checksum)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Beatmap>> GetByBeatmapSetIdAsync(int beatmapSetId)
    {
        return await context.Beatmaps
            .Where(b => b.BeatmapSetId == beatmapSetId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Beatmap>> GetByMapperIdAsync(int mapperId)
    {
        return await context.Beatmaps
            .Where(b => b.MapperId == mapperId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Beatmap> GetOrFetchByIdAsync(int beatmapId, IFetcher fetcher, CancellationToken cancellationToken = default)
    {
        Beatmap? beatmap = await GetByIdAsync(beatmapId).ConfigureAwait(false);
        return beatmap ?? await fetcher.FetchBeatmapAsync(beatmapId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Beatmap> GetOrFetchByChecksumAsync(string checksum, IFetcher fetcher, CancellationToken cancellationToken = default)
    {
        Beatmap? beatmap = await GetByChecksumAsync(checksum).ConfigureAwait(false);
        return beatmap ?? await fetcher.FetchBeatmapAsync(checksum, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(Beatmap beatmap)
    {
        await context.Beatmaps.AddAsync(beatmap).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Beatmap beatmap)
    {
        context.Beatmaps.Update(beatmap);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Beatmap beatmap)
    {
        context.Beatmaps.Remove(beatmap);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}