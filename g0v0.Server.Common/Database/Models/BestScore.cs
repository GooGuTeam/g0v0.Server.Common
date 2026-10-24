// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents the highest-performance score of a user on one beatmap in one mode, materialised in
/// the <c>best_scores</c> table.
/// </summary>
[Table("best_scores")]
[Index(nameof(UserId), nameof(Mode), nameof(Pp), nameof(ScoreId), Name = "ix_best_scores_user_mode_pp_score")]
[Index(nameof(Mode), nameof(Pp), nameof(ScoreId), Name = "ix_best_scores_mode_pp_score")]
[Index(nameof(BeatmapId), Name = "ix_best_scores_beatmap_id")]
[Index(nameof(Mode), Name = "ix_best_scores_gamemode")]
[Index(nameof(UserId), Name = "ix_best_scores_user_id")]
public class BestScore
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
    /// Gets or sets the performance points awarded by the score.
    /// </summary>
    public float Pp { get; set; }

    /// <summary>
    /// Gets or sets the accuracy of the score, in the <c>0</c>-<c>1</c> range.
    /// </summary>
    public float Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the referenced score.
    /// </summary>
    [ForeignKey(nameof(ScoreId))]
    public Score? Score { get; set; }
}