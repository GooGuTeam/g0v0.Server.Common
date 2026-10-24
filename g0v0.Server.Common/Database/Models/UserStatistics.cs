// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents the per-mode performance statistics of a user in the <c>user_statistics</c> table,
/// mirroring the user statistics model of the osu!lazer API.
/// </summary>
/// <remarks>
/// Rows are keyed by the composite (<c>user_id</c>, <c>mode</c>) primary key; there is no surrogate
/// <c>id</c> column. <see cref="Mode"/> uses the same integer encoding as <see cref="Score.Mode"/>.
/// </remarks>
[Table("user_statistics")]
[Index(nameof(UserId), Name = "ix_user_statistics_user_id")]
[Index(nameof(Mode), nameof(Pp), Name = "ix_user_statistics_mode_pp")]
public class UserStatistics
{
    /// <summary>
    /// Gets or sets the ID of the user the statistics belong to.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the ruleset ID the statistics are tracked for
    /// (0-3 = base rulesets, 4 = osu!relax, 5 = osu!autopilot, 6 = taiko relax, 7 = catch relax, 10-14 = custom rulesets).
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Gets or sets the number of 100 (ok) hit results.
    /// </summary>
    public long Count100 { get; set; }

    /// <summary>
    /// Gets or sets the number of 300 (great) hit results.
    /// </summary>
    public long Count300 { get; set; }

    /// <summary>
    /// Gets or sets the number of 50 (meh) hit results.
    /// </summary>
    public long Count50 { get; set; }

    /// <summary>
    /// Gets or sets the number of missed hit results.
    /// </summary>
    public long CountMiss { get; set; }

    /// <summary>
    /// Gets or sets the total performance points of the user in the mode.
    /// </summary>
    public double Pp { get; set; }

    /// <summary>
    /// Gets or sets the total ranked score of the user in the mode.
    /// </summary>
    public long RankedScore { get; set; }

    /// <summary>
    /// Gets or sets the overall hit accuracy of the user in the mode.
    /// </summary>
    public double HitAccuracy { get; set; }

    /// <summary>
    /// Gets or sets the total score of the user in the mode.
    /// </summary>
    public long TotalScore { get; set; }

    /// <summary>
    /// Gets or sets the total number of hits of the user in the mode.
    /// </summary>
    public long TotalHits { get; set; }

    /// <summary>
    /// Gets or sets the maximum combo achieved by the user in the mode.
    /// </summary>
    public int MaximumCombo { get; set; }

    /// <summary>
    /// Gets or sets the number of plays of the user in the mode.
    /// </summary>
    public int PlayCount { get; set; }

    /// <summary>
    /// Gets or sets the total play time in seconds of the user in the mode.
    /// </summary>
    public long PlayTime { get; set; }

    /// <summary>
    /// Gets or sets the number of replays of the user watched by others in the mode.
    /// </summary>
    public int ReplaysWatchedByOthers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the statistics are ranked.
    /// </summary>
    public bool IsRanked { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of SS grade scores of the user in the mode.
    /// </summary>
    public int GradeSs { get; set; }

    /// <summary>
    /// Gets or sets the number of SSH grade scores of the user in the mode.
    /// </summary>
    public int GradeSsh { get; set; }

    /// <summary>
    /// Gets or sets the number of S grade scores of the user in the mode.
    /// </summary>
    public int GradeS { get; set; }

    /// <summary>
    /// Gets or sets the number of SH grade scores of the user in the mode.
    /// </summary>
    public int GradeSh { get; set; }

    /// <summary>
    /// Gets or sets the number of A grade scores of the user in the mode.
    /// </summary>
    public int GradeA { get; set; }

    /// <summary>
    /// Gets or sets the current level of the user in the mode, with the fractional
    /// part carrying the progress towards the next level.
    /// </summary>
    public double LevelCurrent { get; set; } = 1;
}