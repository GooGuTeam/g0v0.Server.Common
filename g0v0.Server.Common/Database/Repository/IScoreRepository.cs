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
    /// <param name="scoreId">
    /// The score ID.
    /// </param>
    /// <returns>
    /// The matching score if found; otherwise, <see langword="null"/>.
    /// </returns>
    Task<Score?> GetByIdAsync(long scoreId);

    /// <summary>
    /// Gets all scores on a beatmap.
    /// </summary>
    /// <param name="beatmapId">
    /// The beatmap ID.
    /// </param>
    /// <returns>
    /// A list of scores submitted on the beatmap.
    /// </returns>
    Task<IReadOnlyList<Score>> GetByBeatmapIdAsync(int beatmapId);

    /// <summary>
    /// Gets passing scores associated with a multiplayer room.
    /// </summary>
    /// <param name="roomId">
    /// The room ID.
    /// </param>
    /// <returns>
    /// The passing scores for the room's playlist items.
    /// </returns>
    Task<IReadOnlyList<Score>> GetPassingScoresForRoomAsync(long roomId);

    /// <summary>
    /// Gets all scores for a user.
    /// </summary>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <returns>
    /// A list of scores belonging to the user.
    /// </returns>
    Task<IReadOnlyList<Score>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Gets recent scores for a user and mode, ordered by end time descending.
    /// </summary>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <param name="mode">
    /// The legacy mode integer.
    /// </param>
    /// <param name="limit">
    /// The maximum number of results to return.
    /// </param>
    /// <returns>
    /// The most recent scores matching the filter.
    /// </returns>
    Task<IReadOnlyList<Score>> GetRecentByUserIdAndModeAsync(int userId, int mode, int limit);

    /// <summary>
    /// Gets best scores for a user and mode, ordered by PP descending.
    /// </summary>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <param name="mode">
    /// The legacy mode integer.
    /// </param>
    /// <param name="limit">
    /// The maximum number of results to return.
    /// </param>
    /// <returns>
    /// The highest-PP scores matching the filter.
    /// </returns>
    Task<IReadOnlyList<Score>> GetBestByUserIdAndModeAsync(int userId, int mode, int limit);

    /// <summary>
    /// Reads the leaderboard of a beatmap, best score first, one row per user.
    /// </summary>
    /// <param name="beatmapId">
    /// The beatmap ID.
    /// </param>
    /// <param name="mode">
    /// The stored ruleset ID, including relax and autopilot variants.
    /// </param>
    /// <param name="limit">
    /// The maximum number of rows.
    /// </param>
    /// <param name="legacyOnly">
    /// Whether only stable scores, the rows with a legacy score value, are considered.
    /// </param>
    /// <param name="userIds">
    /// The users to restrict the leaderboard to, or <see langword="null"/> for everyone.
    /// </param>
    /// <param name="countryCode">
    /// The two-letter country to restrict the leaderboard to, or <see langword="null"/>.
    /// </param>
    /// <param name="modAcronyms">
    /// The exact mod set every row must carry, or <see langword="null"/> for any mods.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The leaderboard rows ordered by score value.
    /// </returns>
    Task<IReadOnlyList<Score>> GetBeatmapLeaderboardAsync(
        int beatmapId,
        int mode,
        int limit,
        bool legacyOnly = false,
        IReadOnlyCollection<int>? userIds = null,
        string? countryCode = null,
        IReadOnlyCollection<string>? modAcronyms = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the distinct users on the leaderboard of a beatmap.
    /// </summary>
    /// <param name="beatmapId">
    /// The beatmap ID.
    /// </param>
    /// <param name="mode">
    /// The stored ruleset ID.
    /// </param>
    /// <param name="legacyOnly">
    /// Whether only stable scores are counted.
    /// </param>
    /// <param name="userIds">
    /// The users to restrict the count to, or <see langword="null"/> for everyone.
    /// </param>
    /// <param name="countryCode">
    /// The two-letter country to restrict the count to, or <see langword="null"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The number of users with a passing score on the beatmap.
    /// </returns>
    Task<int> CountBeatmapLeaderboardAsync(
        int beatmapId,
        int mode,
        bool legacyOnly = false,
        IReadOnlyCollection<int>? userIds = null,
        string? countryCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the best score of one user on a beatmap.
    /// </summary>
    /// <param name="beatmapId">
    /// The beatmap ID.
    /// </param>
    /// <param name="mode">
    /// The stored ruleset ID.
    /// </param>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <param name="legacyOnly">
    /// Whether only stable scores are considered.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The best score, or <see langword="null"/> when the user has not played the beatmap.
    /// </returns>
    Task<Score?> GetBestBeatmapScoreAsync(
        int beatmapId,
        int mode,
        int userId,
        bool legacyOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the one-based position a user holds on the leaderboard of a beatmap.
    /// </summary>
    /// <param name="beatmapId">
    /// The beatmap ID.
    /// </param>
    /// <param name="mode">
    /// The stored ruleset ID.
    /// </param>
    /// <param name="userId">
    /// The user ID.
    /// </param>
    /// <param name="legacyOnly">
    /// Whether only stable scores are considered.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The position, or 0 when the user is not on the leaderboard.
    /// </returns>
    Task<int> GetBeatmapLeaderboardRankAsync(
        int beatmapId,
        int mode,
        int userId,
        bool legacyOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the score attached to a score upload token.
    /// </summary>
    /// <param name="scoreToken">
    /// The score token ID.
    /// </param>
    /// <returns>
    /// The matching score if the token has been completed; otherwise, <see langword="null"/>.
    /// </returns>
    Task<Score?> GetScoreByToken(long scoreToken);

    /// <summary>
    /// Creates a new score record.
    /// </summary>
    /// <param name="score">
    /// The score to create.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task CreateAsync(Score score);

    /// <summary>
    /// Updates an existing score record.
    /// </summary>
    /// <param name="score">
    /// The score to update.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task UpdateAsync(Score score);

    /// <summary>
    /// Removes a score record.
    /// </summary>
    /// <param name="score">
    /// The score to remove.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task DeleteAsync(Score score);

    /// <summary>
    /// Marks a score as having an uploaded replay.
    /// </summary>
    /// <param name="scoreId">
    /// The score ID.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task MarkScoreHasReplay(long scoreId);

    /// <summary>
    /// Reads a page of unprocessed scores, optionally filtered by source type.
    /// </summary>
    /// <param name="afterId">
    /// The exclusive pagination cursor.
    /// </param>
    /// <param name="limit">
    /// The page size.
    /// </param>
    /// <param name="type">
    /// An optional source type, such as stable or lazer.
    /// </param>
    /// <returns>
    /// Unprocessed scores ordered by ID.
    /// </returns>
    Task<IReadOnlyList<Score>> GetUnprocessedScoresAsync(long afterId, int limit, string? type = null);

    /// <summary>
    /// Saves calculated score fields without overwriting concurrent replay uploads.
    /// </summary>
    /// <param name="score">
    /// The successfully calculated score.
    /// </param>
    /// <param name="playTime">
    /// Actual play duration in seconds.
    /// </param>
    /// <returns>
    /// A task representing persistence.
    /// </returns>
    Task CompleteProcessingAsync(Score score, int playTime = 0);
}