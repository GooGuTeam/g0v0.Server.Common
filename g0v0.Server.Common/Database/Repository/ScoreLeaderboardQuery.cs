// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Shared leaderboard queries for <see cref="IScoreRepository"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// A beatmap leaderboard is read from the materialised <c>total_score_best_scores</c> table: one row
/// per user, beatmap, mode and mod set. Because a user may hold several rows on the same difficulty
/// when they played different mod sets, the query reads the candidates ordered by score and keeps the
/// highest row of each user. A bounded candidate window keeps that a single round trip.
/// </para>
/// <para>
/// Stable clients may only use stable scores, which are the rows carrying a legacy
/// <c>classic_total_score</c>; <c>legacyOnly</c> joins the referenced score and selects exactly those.
/// </para>
/// </remarks>
public static class ScoreLeaderboardQuery
{
    /// <summary>
    /// The factor applied to the requested page size when reading candidates, so that users with
    /// several entries on the same beatmap still leave a full page after de-duplication.
    /// </summary>
    public const int CandidateWindowFactor = 8;

    /// <summary>
    /// The hard cap applied to the candidate window of a leaderboard query.
    /// </summary>
    public const int MaxCandidateWindow = 1000;

    /// <summary>
    /// Reads the leaderboard of a beatmap, best score first, one row per user.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <param name="mode">The stored ruleset ID, relax and autopilot variants included.</param>
    /// <param name="limit">The maximum number of rows.</param>
    /// <param name="legacyOnly">Whether only stable scores are considered.</param>
    /// <param name="userIds">The users to restrict to, or <see langword="null"/> for everyone.</param>
    /// <param name="countryCode">The two-letter country to restrict to, or <see langword="null"/>.</param>
    /// <param name="modAcronyms">The exact mod set a row must carry, or <see langword="null"/> for any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The leaderboard rows.</returns>
    public static async Task<IReadOnlyList<Score>> GetLeaderboardAsync(
        DbContext context,
        int beatmapId,
        int mode,
        int limit,
        bool legacyOnly = false,
        IReadOnlyCollection<int>? userIds = null,
        string? countryCode = null,
        IReadOnlyCollection<string>? modAcronyms = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (limit <= 0)
        {
            return [];
        }

        List<Score> candidates = await Filter(context, beatmapId, mode, legacyOnly, userIds, countryCode)
            .OrderByDescending(entry => entry.TotalScore)
            .ThenBy(entry => entry.ScoreId)
            .Take(CandidateWindow(limit))
            .Select(entry => entry.Score!)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return BestPerUser(candidates, modAcronyms).Take(limit).ToList();
    }

    /// <summary>
    /// Counts the users that have a score on the beatmap.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <param name="mode">The stored ruleset ID.</param>
    /// <param name="legacyOnly">Whether only stable scores are counted.</param>
    /// <param name="userIds">The users to restrict the count to, or <see langword="null"/> for everyone.</param>
    /// <param name="countryCode">The two-letter country to restrict the count to, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of distinct users on the leaderboard.</returns>
    public static Task<int> CountLeaderboardAsync(
        DbContext context,
        int beatmapId,
        int mode,
        bool legacyOnly = false,
        IReadOnlyCollection<int>? userIds = null,
        string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Filter(context, beatmapId, mode, legacyOnly, userIds, countryCode)
            .Select(entry => entry.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Reads the best score of one user on a beatmap.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <param name="mode">The stored ruleset ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="legacyOnly">Whether only stable scores are considered.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's best score, or <see langword="null"/> when they have not set one.</returns>
    public static Task<Score?> GetBestScoreAsync(
        DbContext context,
        int beatmapId,
        int mode,
        int userId,
        bool legacyOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Filter(context, beatmapId, mode, legacyOnly, [userId], countryCode: null)
            .OrderByDescending(entry => entry.TotalScore)
            .ThenBy(entry => entry.ScoreId)
            .Select(entry => entry.Score)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the one-based position a user holds on the leaderboard of a beatmap.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="beatmapId">The beatmap ID.</param>
    /// <param name="mode">The stored ruleset ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="legacyOnly">Whether only stable scores are considered.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The position, or 0 when the user is not on the leaderboard.</returns>
    public static async Task<int> GetLeaderboardRankAsync(
        DbContext context,
        int beatmapId,
        int mode,
        int userId,
        bool legacyOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        IReadOnlyList<Score> leaderboard = await GetLeaderboardAsync(
            context,
            beatmapId,
            mode,
            MaxCandidateWindow,
            legacyOnly,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        int index = leaderboard.ToList().FindIndex(score => score.UserId == userId);
        return index < 0 ? 0 : index + 1;
    }

    private static IQueryable<LeaderboardScore> Filter(
        DbContext context,
        int beatmapId,
        int mode,
        bool legacyOnly,
        IReadOnlyCollection<int>? userIds,
        string? countryCode)
    {
        IQueryable<LeaderboardScore> query = context.Set<LeaderboardScore>()
            .Where(entry => entry.BeatmapId == beatmapId && entry.Mode == mode);

        if (legacyOnly)
        {
            query = query.Where(entry => entry.Score!.ClassicTotalScore > 0);
        }

        if (userIds is { Count: > 0 })
        {
            int[] ids = userIds.ToArray();
            query = query.Where(entry => ((IEnumerable<int>)ids).Contains(entry.UserId));
        }

        if (!string.IsNullOrEmpty(countryCode))
        {
            query = query.Where(entry => context.Set<User>().Any(user => user.Id == entry.UserId && user.CountryCode == countryCode));
        }

        return query;
    }

    private static IEnumerable<Score> BestPerUser(IEnumerable<Score> ordered, IReadOnlyCollection<string>? modAcronyms)
    {
        HashSet<int> seen = [];
        foreach (Score score in ordered.Where(score => modAcronyms is not { Count: > 0 } || HasExactMods(score, modAcronyms)).Where(score => seen.Add(score.UserId)))
        {
            yield return score;
        }
    }

    private static bool HasExactMods(Score score, IReadOnlyCollection<string> modAcronyms)
    {
        HashSet<string> actual = score.Mods.Select(mod => mod.Acronym).ToHashSet(StringComparer.Ordinal);
        HashSet<string> expected = modAcronyms.ToHashSet(StringComparer.Ordinal);
        return actual.SetEquals(expected);
    }

    private static int CandidateWindow(int limit) => Math.Min(Math.Max(limit, 1) * CandidateWindowFactor, MaxCandidateWindow);
}