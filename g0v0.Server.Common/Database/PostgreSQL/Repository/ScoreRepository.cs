// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IScoreRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class ScoreRepository(PostgreSqlDbContext context) : IScoreRepository, IPostgreSqlRepository
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
    public async Task<IReadOnlyList<Score>> GetPassingScoresForRoomAsync(long roomId)
    {
        return await context.Scores
            .AsNoTracking()
            .Where(s => s.RoomId == roomId && s.PlaylistItemId != null && s.Passed)
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
        return limit <= 0
            ? []
            : (IReadOnlyList<Score>)await context.Scores
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
        return limit <= 0
            ? []
            : (IReadOnlyList<Score>)await context.Scores
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
    public Task<IReadOnlyList<Score>> GetBeatmapLeaderboardAsync(
        int beatmapId,
        int mode,
        int limit,
        bool legacyOnly = false,
        IReadOnlyCollection<int>? userIds = null,
        string? countryCode = null,
        IReadOnlyCollection<string>? modAcronyms = null,
        CancellationToken cancellationToken = default)
        => ScoreLeaderboardQuery.GetLeaderboardAsync(
            context,
            beatmapId,
            mode,
            limit,
            legacyOnly,
            userIds,
            countryCode,
            modAcronyms,
            cancellationToken);

    /// <inheritdoc/>
    public Task<int> CountBeatmapLeaderboardAsync(
        int beatmapId,
        int mode,
        bool legacyOnly = false,
        IReadOnlyCollection<int>? userIds = null,
        string? countryCode = null,
        CancellationToken cancellationToken = default)
        => ScoreLeaderboardQuery.CountLeaderboardAsync(context, beatmapId, mode, legacyOnly, userIds, countryCode, cancellationToken);

    /// <inheritdoc/>
    public Task<Score?> GetBestBeatmapScoreAsync(
        int beatmapId,
        int mode,
        int userId,
        bool legacyOnly = false,
        CancellationToken cancellationToken = default)
        => ScoreLeaderboardQuery.GetBestScoreAsync(context, beatmapId, mode, userId, legacyOnly, cancellationToken);

    /// <inheritdoc/>
    public Task<int> GetBeatmapLeaderboardRankAsync(
        int beatmapId,
        int mode,
        int userId,
        bool legacyOnly = false,
        CancellationToken cancellationToken = default)
        => ScoreLeaderboardQuery.GetLeaderboardRankAsync(context, beatmapId, mode, userId, legacyOnly, cancellationToken);

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

    /// <inheritdoc/>
    public async Task MarkScoreHasReplay(long scoreId)
    {
        Score? score = await context.Scores
            .FirstOrDefaultAsync(s => s.Id == scoreId)
            .ConfigureAwait(false);

        if (score == null)
        {
            return;
        }

        score.HasReplay = true;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Score>> GetUnprocessedScoresAsync(long afterId, int limit, string? type = null)
        => limit <= 0 ? [] : await context.Scores.AsNoTracking()
            .Where(s => s.Id > afterId && !s.Processed && (type == null || s.Type == type))
            .OrderBy(s => s.Id).Take(limit).ToListAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public Task CompleteProcessingAsync(Score score, int playTime = 0)
        => ScoreProcessingPersistence.CompleteAsync(context, score, playTime);
}