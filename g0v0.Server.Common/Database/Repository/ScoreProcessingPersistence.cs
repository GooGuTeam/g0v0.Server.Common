// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Data;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using Beatmap = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Database.Repository;

internal static class ScoreProcessingPersistence
{
    internal static async Task CompleteAsync(DbContext context, Score score, int playTime)
    {
        await using IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable).ConfigureAwait(false) : null;
        Score stored = await context.Set<Score>().FirstAsync(s => s.Id == score.Id).ConfigureAwait(false);
        if (stored.Processed)
        {
            return;
        }

        UserStatistics? statistics = await context.Set<UserStatistics>().FirstOrDefaultAsync(s => s.UserId == score.UserId && s.Mode == score.Mode).ConfigureAwait(false);
        if (statistics == null)
        {
            statistics = new UserStatistics { UserId = score.UserId, Mode = score.Mode };
            context.Set<UserStatistics>().Add(statistics);
        }

        List<Score> history = await context.Set<Score>().AsNoTracking()
            .Where(s => s.UserId == score.UserId && s.Mode == score.Mode && s.Processed && s.Id != score.Id)
            .ToListAsync().ConfigureAwait(false);
        UserScoreStatisticsUtils.Apply(statistics, score, history, playTime);
        stored.Accuracy = score.Accuracy;
        stored.Rank = score.Rank;
        stored.TotalScore = score.TotalScore;
        stored.TotalScoreWithoutMods = score.TotalScoreWithoutMods;
        stored.ClassicTotalScoreWithoutMods = score.ClassicTotalScoreWithoutMods;
        stored.MaximumStatistics = score.MaximumStatistics;
        stored.Ranked = score.Ranked;
        stored.Pp = score.Pp;
        stored.Processed = true;

        Beatmap? beatmap = await context.Set<Beatmap>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == stored.BeatmapId).ConfigureAwait(false);
        bool hasLeaderboard = beatmap is not null && HasLeaderboard(beatmap.Status);
        await MaintainBestScoreAsync(context, stored).ConfigureAwait(false);
        await MaintainLeaderboardScoreBestAsync(context, stored, hasLeaderboard).ConfigureAwait(false);
        await IncrementPlaycountsAsync(context, stored, playTime).ConfigureAwait(false);

        await context.SaveChangesAsync().ConfigureAwait(false);
        if (transaction != null)
        {
            await transaction.CommitAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Decides whether a beatmap status exposes a score leaderboard.
    /// </summary>
    /// <param name="status">The stored online status.</param>
    /// <returns>Whether leaderboard rows are kept for the beatmap.</returns>
    private static bool HasLeaderboard(BeatmapOnlineStatus status) =>
        status is BeatmapOnlineStatus.Ranked or BeatmapOnlineStatus.Approved or BeatmapOnlineStatus.Qualified or BeatmapOnlineStatus.Loved;

    /// <summary>
    /// Replaces the performance entry of the user on the beatmap when this score beats it.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="score">The completed score.</param>
    /// <returns>A task representing the change.</returns>
    /// <remarks>
    /// Only a passing, ranked score that may award performance produces an entry. The entry is keyed
    /// by the score ID, so a better score replaces the previous row instead of adding one.
    /// </remarks>
    private static async Task MaintainBestScoreAsync(DbContext context, Score score)
    {
        if (!score.Passed || !score.Ranked || score.Pp <= 0)
        {
            return;
        }

        BestScore? previous = await context.Set<BestScore>()
            .FirstOrDefaultAsync(b => b.UserId == score.UserId && b.BeatmapId == score.BeatmapId && b.Mode == score.Mode)
            .ConfigureAwait(false);
        if (previous is not null && previous.Pp >= score.Pp)
        {
            return;
        }

        if (previous is not null)
        {
            context.Set<BestScore>().Remove(previous);
        }

        context.Set<BestScore>().Add(new BestScore
        {
            ScoreId = score.Id,
            UserId = score.UserId,
            BeatmapId = score.BeatmapId,
            Mode = score.Mode,
            Pp = (float)score.Pp,
            Accuracy = (float)score.Accuracy
        });
    }

    /// <summary>
    /// Maintains the leaderboard entry of the user on the beatmap for the mod set of this score.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="score">The completed score.</param>
    /// <param name="hasLeaderboard">Whether the beatmap exposes a leaderboard at all.</param>
    /// <returns>A task representing the change.</returns>
    /// <remarks>
    /// The table keeps the best entry of a user per mod set, so a better score with the same mods
    /// replaces that entry and a score with a new mod set adds one. When the replacement is the
    /// overall best of the user, the previous overall best entry is removed, keeping a single top row
    /// per user.
    /// </remarks>
    private static async Task MaintainLeaderboardScoreBestAsync(DbContext context, Score score, bool hasLeaderboard)
    {
        if (!hasLeaderboard || !score.Passed)
        {
            return;
        }

        IList<string> mods = ModAcronyms(score.Mods);
        List<LeaderboardScore> entries = await context.Set<LeaderboardScore>()
            .Where(b => b.UserId == score.UserId && b.BeatmapId == score.BeatmapId && b.Mode == score.Mode)
            .ToListAsync().ConfigureAwait(false);

        LeaderboardScore? overall = entries
            .OrderByDescending(entry => entry.TotalScore)
            .ThenByDescending(entry => entry.ScoreId)
            .FirstOrDefault();
        LeaderboardScore? sameMods = entries.FirstOrDefault(entry => SameModSet(entry.Mods, mods));
        if (overall is null)
        {
            AddEntry(context, score, mods);
            return;
        }

        if (ReferenceEquals(overall, sameMods))
        {
            if (score.TotalScore > overall.TotalScore)
            {
                ReplaceEntry(context, overall, score, mods);
            }

            return;
        }

        // The best entry of the user carries different mods, so a better score displaces it; the
        // entry of this mod set is then either created or improved.
        if (score.TotalScore > overall.TotalScore)
        {
            context.Set<LeaderboardScore>().Remove(overall);
        }

        if (sameMods is null)
        {
            AddEntry(context, score, mods);
        }
        else if (score.TotalScore > sameMods.TotalScore)
        {
            ReplaceEntry(context, sameMods, score, mods);
        }
    }

    /// <summary>
    /// Increments the per-beatmap and per-month play counters of a counted play.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="score">The completed score.</param>
    /// <param name="playTime">Actual play duration in seconds.</param>
    /// <returns>A task representing the change.</returns>
    private static async Task IncrementPlaycountsAsync(DbContext context, Score score, int playTime)
    {
        if (!UserScoreStatisticsUtils.IsValidPlay(score, playTime))
        {
            return;
        }

        BeatmapPlaycount? beatmapPlaycount = await context.Set<BeatmapPlaycount>()
            .FirstOrDefaultAsync(c => c.UserId == score.UserId && c.BeatmapId == score.BeatmapId)
            .ConfigureAwait(false);
        if (beatmapPlaycount is null)
        {
            context.Set<BeatmapPlaycount>().Add(new BeatmapPlaycount
            {
                UserId = score.UserId,
                BeatmapId = score.BeatmapId,
                Playcount = 1
            });
        }
        else
        {
            beatmapPlaycount.Playcount++;
        }

        DateTime endedAt = score.EndedAt.UtcDateTime;
        MonthlyPlaycount? monthlyPlaycount = await context.Set<MonthlyPlaycount>()
            .FirstOrDefaultAsync(c => c.UserId == score.UserId && c.Year == endedAt.Year && c.Month == endedAt.Month)
            .ConfigureAwait(false);
        if (monthlyPlaycount is null)
        {
            context.Set<MonthlyPlaycount>().Add(new MonthlyPlaycount
            {
                UserId = score.UserId,
                Year = endedAt.Year,
                Month = endedAt.Month,
                Count = 1
            });
        }
        else
        {
            monthlyPlaycount.Count++;
        }
    }

    private static List<string> ModAcronyms(IEnumerable<APIMod> mods) => mods
        .Select(mod => mod.Acronym)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(acronym => acronym, StringComparer.Ordinal)
        .ToList();

    private static bool SameModSet(IList<string> stored, IList<string> current)
    {
        // The mod set is compared as an ordered list, which is how the leaderboard's exact-mod
        // filter reads it back from the JSON column.
        string[] left = stored.OrderBy(acronym => acronym, StringComparer.Ordinal).ToArray();
        string[] right = current.OrderBy(acronym => acronym, StringComparer.Ordinal).ToArray();
        return left.SequenceEqual(right, StringComparer.Ordinal);
    }

    private static void AddEntry(DbContext context, Score score, IList<string> mods) =>
        context.Set<LeaderboardScore>().Add(new LeaderboardScore
        {
            ScoreId = score.Id,
            UserId = score.UserId,
            BeatmapId = score.BeatmapId,
            Mode = score.Mode,
            TotalScore = score.TotalScore,
            Mods = mods,
            Rank = score.Rank
        });

    private static void ReplaceEntry(DbContext context, LeaderboardScore entry, Score score, IList<string> mods)
    {
        // The entry is keyed by the score it points at, so a better score is a new row: the previous
        // one is deleted in the same unit of work.
        context.Set<LeaderboardScore>().Remove(entry);
        AddEntry(context, score, mods);
    }
}