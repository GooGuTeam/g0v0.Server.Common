// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>
/// MySQL-backed implementation of <see cref="IScoreRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class ScoreRepository(MysqlDbContext context) : IScoreRepository, IMySqlRepository
{
    /// <inheritdoc/>
    public async Task<Score?> GetByIdAsync(long scoreId)
    {
        return await context.Scores
            .FirstOrDefaultAsync(s => s.Id == scoreId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Score>> GetByBeatmapIdAsync(int beatmapId)
    {
        return await context.Scores
            .Where(s => s.BeatmapId == beatmapId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Score>> GetByUserIdAsync(int userId)
    {
        return await context.Scores
            .Where(s => s.UserId == userId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Score>> GetRecentByUserIdAndModeAsync(int userId, int mode, int limit)
    {
        if (limit <= 0)
        {
            return [];
        }

        return await context.Scores
            .Where(s => s.UserId == userId && s.Mode == mode)
            .OrderByDescending(s => s.EndedAt)
            .ThenByDescending(s => s.Id)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Score>> GetBestByUserIdAndModeAsync(int userId, int mode, int limit)
    {
        if (limit <= 0)
        {
            return [];
        }

        return await context.Scores
            .Where(s => s.UserId == userId && s.Mode == mode)
            .OrderByDescending(s => s.Pp)
            .ThenByDescending(s => s.Id)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Score?> GetScoreByToken(long scoreToken)
    {
        return await context.ScoreTokens
            .Where(t => t.Id == scoreToken && t.ScoreId != null)
            .Select(t => t.Score)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(Score score)
    {
        await context.Scores.AddAsync(score).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Score score)
    {
        context.Scores.Update(score);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Score score)
    {
        context.Scores.Remove(score);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task MarkScoreHasReplay(long scoreId)
    {
        var score = await context.Scores
            .FirstOrDefaultAsync(s => s.Id == scoreId)
            .ConfigureAwait(false);

        if (score == null)
        {
            return;
        }

        score.HasReplay = true;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}