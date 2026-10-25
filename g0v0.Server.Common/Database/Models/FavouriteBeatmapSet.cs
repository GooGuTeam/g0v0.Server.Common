// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Records a user's favourite beatmap set.
/// </summary>
[Table("favourite_beatmapset")]
[PrimaryKey(nameof(UserId), nameof(BeatmapSetId))]
public class FavouriteBeatmapSet
{
    /// <summary>Gets or sets the owner ID.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the beatmap set ID.</summary>
    public int BeatmapSetId { get; set; }

    /// <summary>Gets or sets when the favourite was added.</summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>Gets or sets the owner.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets the beatmap set.</summary>
    public BeatmapSet? BeatmapSet { get; set; }
}