// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Repository;

internal static class AchievementPersistence
{
    internal static async Task<IReadOnlyList<int>> UnlockAsync(DbContext context, int userId, IReadOnlyList<int> ids, DateTimeOffset achievedAt, CancellationToken cancellationToken)
    {
        int[] remaining = ids.Distinct().ToArray();
        while (remaining.Length > 0)
        {
            int[] owned = await context.Set<UserAchievement>().Where(item => item.UserId == userId)
                .Select(item => item.AchievementId).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            int[] pending = remaining.Except(owned).ToArray();
            if (pending.Length == 0)
            {
                break;
            }

            UserAchievement[] records = pending.Select(id => new UserAchievement { UserId = userId, AchievementId = id, AchievedAt = achievedAt }).ToArray();
            context.Set<UserAchievement>().AddRange(records);
            try
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return pending;
            }
            catch (DbUpdateException)
            {
                foreach (UserAchievement record in records)
                {
                    context.Entry(record).State = EntityState.Detached;
                }

                // A unique-key race is retryable; other database failures must remain visible.
                if (!await context.Set<UserAchievement>().AnyAsync(item => item.UserId == userId && pending.Contains(item.AchievementId), cancellationToken).ConfigureAwait(false))
                {
                    throw;
                }

                remaining = pending;
            }
        }

        return [];
    }
}