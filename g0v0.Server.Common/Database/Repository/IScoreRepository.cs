// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for score records.
/// </summary>
public interface IScoreRepository
{
    /// <summary>
    /// Gets a score by its unique identifier.
    /// </summary>
    /// <param name="scoreId">The score ID.</param>
    /// <returns>The matching score if found; otherwise, <see langword="null"/>.</returns>
    Task<Score?> GetByIdAsync(long scoreId);

    /// <summary>
    /// Gets all scores on a beatmap.
    /// </summary>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <returns>A list of scores submitted on the beatmap.</returns>
    Task<IReadOnlyList<Score>> GetByBeatmapIdAsync(int beatmapId);

    /// <summary>
    /// Gets all scores for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of scores belonging to the user.</returns>
    Task<IReadOnlyList<Score>> GetByUserIdAsync(long userId);

    /// <summary>
    /// Gets recent scores for a user and mode, ordered by end time descending.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="mode">The legacy mode integer.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>The most recent scores matching the filter.</returns>
    Task<IReadOnlyList<Score>> GetRecentByUserIdAndModeAsync(long userId, int mode, int limit);

    /// <summary>
    /// Gets best scores for a user and mode, ordered by PP descending.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="mode">The legacy mode integer.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>The highest-PP scores matching the filter.</returns>
    Task<IReadOnlyList<Score>> GetBestByUserIdAndModeAsync(long userId, int mode, int limit);

    /// <summary>
    /// Gets the score attached to a score upload token.
    /// </summary>
    /// <param name="scoreToken">The score token ID.</param>
    /// <returns>The matching score if the token has been completed; otherwise, <see langword="null"/>.</returns>
    Task<Score?> GetScoreByToken(long scoreToken);

    /// <summary>
    /// Creates a new score record.
    /// </summary>
    /// <param name="score">The score to create.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateAsync(Score score);

    /// <summary>
    /// Updates an existing score record.
    /// </summary>
    /// <param name="score">The score to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateAsync(Score score);

    /// <summary>
    /// Removes a score record.
    /// </summary>
    /// <param name="score">The score to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(Score score);

    /// <summary>
    /// Marks a score as having an uploaded replay.
    /// </summary>
    /// <param name="scoreId">The score ID.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task MarkScoreHasReplay(long scoreId);
}