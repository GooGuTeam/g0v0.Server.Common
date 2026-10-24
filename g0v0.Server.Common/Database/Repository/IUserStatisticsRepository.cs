// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for per-mode user performance statistics,
/// mirroring the user statistics database helpers of the osu!lazer API.
/// </summary>
public interface IUserStatisticsRepository
{
    /// <summary>
    /// Gets the statistics of a user for a single mode.
    /// </summary>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <param name="mode">
    /// The ruleset ID.
    /// </param>
    /// <returns>
    /// The matching statistics if found; otherwise, <see langword="null"/>.
    /// </returns>
    Task<UserStatistics?> GetByUserIdAndModeAsync(int userId, int mode);

    /// <summary>
    /// Gets all statistics rows of a user, one per mode.
    /// </summary>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <returns>
    /// The statistics rows of the user.
    /// </returns>
    Task<IReadOnlyList<UserStatistics>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Computes the global rank of a user's statistics within a mode by performance points.
    /// </summary>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <param name="mode">
    /// The ruleset ID.
    /// </param>
    /// <returns>
    /// The 1-based rank, or <see langword="null"/> when the statistics do not exist or are unranked.
    /// </returns>
    Task<int?> GetGlobalRankAsync(int userId, int mode);

    /// <summary>
    /// Creates a new statistics row.
    /// </summary>
    /// <param name="statistics">
    /// The statistics to create.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task CreateAsync(UserStatistics statistics);

    /// <summary>
    /// Updates an existing statistics row.
    /// </summary>
    /// <param name="statistics">
    /// The statistics to update.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task UpdateAsync(UserStatistics statistics);
}