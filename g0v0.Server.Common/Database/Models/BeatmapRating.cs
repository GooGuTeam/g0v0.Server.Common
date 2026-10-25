// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Records a user's single rating for a beatmap set.
/// </summary>
[Table("beatmap_ratings")]
[PrimaryKey(nameof(BeatmapSetId), nameof(UserId))]
public class BeatmapRating
{
    /// <summary>Gets or sets the beatmap set ID.</summary>
    public int BeatmapSetId { get; set; }

    /// <summary>Gets or sets the voting user ID.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the rating, from one to ten.</summary>
    public int Rating { get; set; }

    /// <summary>Gets or sets the beatmap set.</summary>
    [ForeignKey(nameof(BeatmapSetId))]
    public BeatmapSet? BeatmapSet { get; set; }

    /// <summary>Gets or sets the voting user.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}