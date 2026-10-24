// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// Persists per-mode user performance statistics in the v2 PostgreSQL schema.
/// </summary>
/// <param name="context">The PostgreSQL database context.</param>
public class UserStatisticsRepository(PostgreSqlDbContext context) : IUserStatisticsRepository, IPostgreSqlRepository
{
    /// <inheritdoc/>
    public async Task<UserStatistics?> GetByUserIdAndModeAsync(int userId, int mode)
    {
        return await context.UserStatistics
            .FirstOrDefaultAsync(statistics => statistics.UserId == userId && statistics.Mode == mode)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserStatistics>> GetByUserIdAsync(int userId)
    {
        return await context.UserStatistics
            .Where(statistics => statistics.UserId == userId)
            .OrderBy(statistics => statistics.Mode)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int?> GetGlobalRankAsync(int userId, int mode)
    {
        UserStatistics? statistics = await GetByUserIdAndModeAsync(userId, mode).ConfigureAwait(false);

        if (statistics == null || !statistics.IsRanked || statistics.Pp <= 0)
        {
            return null;
        }

        int betterPlayers = await context.UserStatistics
            .CountAsync(candidate => candidate.Mode == mode
                && candidate.IsRanked
                && candidate.Pp > statistics.Pp)
            .ConfigureAwait(false);

        return betterPlayers + 1;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(UserStatistics statistics)
    {
        await context.UserStatistics.AddAsync(statistics).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(UserStatistics statistics)
    {
        context.UserStatistics.Update(statistics);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}