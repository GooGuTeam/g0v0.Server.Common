// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using osu.Game.Scoring;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents the highest-scoring entry of a user on one beatmap in one mode, materialised in the
/// <c>leaderboard_scores</c> table.
/// </summary>
/// <remarks>
/// <para>
/// The table is the source of every beatmap leaderboard: a row exists per
/// (<c>user_id</c>, <c>beatmap_id</c>, <c>gamemode</c>, mod set), so a user can hold several rows on
/// the same difficulty when they played different mod combinations. The leaderboard then keeps the
/// highest row of each user.
/// </para>
/// <para>
/// Rows are only written for passed scores on a beatmap that has a leaderboard
/// (ranked, approved, qualified or loved). <see cref="TotalScore"/> stores the standardised score,
/// which is what the leaderboard sorts by; the legacy score the stable client displays stays on the
/// referenced <see cref="Score"/>.
/// </para>
/// </remarks>
[Index(nameof(UserId), nameof(Mode), nameof(ScoreId), Name = "ix_leaderboard_scores_user_mode_score")]
[Index(nameof(BeatmapId), nameof(Mode), nameof(TotalScore), Name = "ix_leaderboard_scores_beatmap_mode_score")]
[Index(nameof(BeatmapId), Name = "ix_leaderboard_scores_beatmap_id")]
[Index(nameof(Mode), Name = "ix_leaderboard_scores_gamemode")]
[Index(nameof(UserId), Name = "ix_leaderboard_scores_user_id")]
public class LeaderboardScore
{
    /// <summary>
    /// Gets or sets the ID of the referenced score, which is also the primary key.
    /// </summary>
    [Key]
    public long ScoreId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user owning the score.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the beatmap ID the score was set on.
    /// </summary>
    public int BeatmapId { get; set; }

    /// <summary>
    /// Gets or sets the stored ruleset ID, relax and autopilot variants included.
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Gets or sets the standardised total score of the entry.
    /// </summary>
    public long TotalScore { get; set; }

    /// <summary>
    /// Gets or sets the sorted, de-duplicated mod acronyms of the entry.
    /// </summary>
    /// <remarks>
    /// The mod set is stored so the mods leaderboard can be answered with an exact match: the client
    /// asks for the scores that carry precisely the mods it reported, not for a superset.
    /// </remarks>
    public IList<string> Mods { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the grade of the entry.
    /// </summary>
    public ScoreRank Rank { get; set; }

    /// <summary>
    /// Gets or sets the referenced score.
    /// </summary>
    [ForeignKey(nameof(ScoreId))]
    public Score? Score { get; set; }
}