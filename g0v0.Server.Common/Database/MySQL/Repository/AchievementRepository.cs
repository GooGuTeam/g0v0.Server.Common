// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>Persists medals using MySQL.</summary>
/// <param name="context">The database context.</param>
public class AchievementRepository(MysqlDbContext context) : IAchievementRepository, IMySqlRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<int>> GetUnlockedIdsAsync(int userId, CancellationToken cancellationToken = default)
        => await context.UserAchievements.Where(item => item.UserId == userId).Select(item => item.AchievementId)
            .OrderBy(id => id).ToArrayAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public Task<IReadOnlyList<int>> UnlockAsync(int userId, IReadOnlyList<int> achievementIds, DateTimeOffset achievedAt, CancellationToken cancellationToken = default)
        => AchievementPersistence.UnlockAsync(context, userId, achievementIds, achievedAt, cancellationToken);
}