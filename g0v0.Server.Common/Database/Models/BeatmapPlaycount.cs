// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents how often one user played one beatmap, materialised in the <c>beatmap_playcounts</c>
/// table.
/// </summary>
[Table("beatmap_playcounts")]
[Index(nameof(UserId), Name = "ix_beatmap_playcounts_user_id")]
[Index(nameof(BeatmapId), Name = "ix_beatmap_playcounts_beatmap_id")]
[Index(nameof(Playcount), nameof(Id), Name = "idx_beatmap_playcounts_playcount_id")]
public class BeatmapPlaycount
{
    /// <summary>
    /// Gets or sets the surrogate primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user that played the beatmap.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the played beatmap.
    /// </summary>
    public int BeatmapId { get; set; }

    /// <summary>
    /// Gets or sets the number of times the user played the beatmap.
    /// </summary>
    public int Playcount { get; set; }
}