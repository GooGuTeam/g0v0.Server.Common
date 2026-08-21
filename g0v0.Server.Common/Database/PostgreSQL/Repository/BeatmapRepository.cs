// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IBeatmapRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class BeatmapRepository(PostgreSqlDbContext context) : IBeatmapRepository, IPostgreSqlRepository
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
    public async Task<IReadOnlyList<Beatmap>> GetByBeatmapSetIdAsync(long beatmapSetId)
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