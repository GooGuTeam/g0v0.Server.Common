// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.Repository;

/// <summary>Persists user medals with one unlock per user and achievement.</summary>
public interface IAchievementRepository
{
    /// <summary>Reads the IDs a user already owns.</summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The unlocked IDs.</returns>
    Task<IReadOnlyList<int>> GetUnlockedIdsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Atomically adds missing medals, preserving the original unlock times.</summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementIds">The eligible IDs.</param>
    /// <param name="achievedAt">The unlock time.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Only the IDs inserted by this call, including under concurrent submissions.</returns>
    Task<IReadOnlyList<int>> UnlockAsync(int userId, IReadOnlyList<int> achievementIds, DateTimeOffset achievedAt, CancellationToken cancellationToken = default);
}