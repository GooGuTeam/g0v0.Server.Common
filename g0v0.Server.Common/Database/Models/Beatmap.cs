// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using osu.Game.Beatmaps;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a beatmap record used by score submission and spectator replay upload flows.
/// </summary>
[Table("beatmaps")]
public class Beatmap
{
    /// <summary>
    /// Gets or sets the beatmap ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the beatmap set ID.
    /// </summary>
    public long BeatmapSetId { get; set; }

    /// <summary>
    /// Gets or sets the public beatmap URL.
    /// </summary>
    [StringLength(255)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the beatmap checksum.
    /// </summary>
    [StringLength(32)]
    public string? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the maximum combo.
    /// </summary>
    public int? MaxCombo { get; set; }

    /// <summary>
    /// Gets or sets the difficulty version name.
    /// </summary>
    [StringLength(255)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legacy ruleset ID.
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Gets or sets the total length in seconds.
    /// </summary>
    public int TotalLength { get; set; }

    /// <summary>
    /// Gets or sets the star difficulty rating.
    /// </summary>
    public double DifficultyRating { get; set; }

    /// <summary>
    /// Gets or sets the online status of the beatmap.
    /// </summary>
    public BeatmapOnlineStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the approach rate.
    /// </summary>
    public float ApproachRate { get; set; }

    /// <summary>
    /// Gets or sets the circle size.
    /// </summary>
    public float CircleSize { get; set; }

    /// <summary>
    /// Gets or sets the health drain rate.
    /// </summary>
    public float HpDrainRate { get; set; }

    /// <summary>
    /// Gets or sets the overall difficulty.
    /// </summary>
    public float OverallDifficulty { get; set; }

    /// <summary>
    /// Gets or sets the beats per minute.
    /// </summary>
    public float Bpm { get; set; }

    /// <summary>
    /// Gets or sets the number of circles.
    /// </summary>
    public int CirclesCount { get; set; }

    /// <summary>
    /// Gets or sets the number of sliders.
    /// </summary>
    public int SlidersCount { get; set; }

    /// <summary>
    /// Gets or sets the number of spinners.
    /// </summary>
    public int SpinnersCount { get; set; }

    /// <summary>
    /// Gets or sets the drain time in seconds.
    /// </summary>
    public int HitLength { get; set; }

    /// <summary>
    /// Gets or sets the mapper user ID.
    /// </summary>
    public int MapperId { get; set; }

    /// <summary>
    /// Gets or sets the soft-delete timestamp.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset? LastUpdate { get; set; }

    /// <summary>
    /// Gets or sets the beatmap file format version used for legacy replay encoding.
    /// </summary>
    public int BeatmapVersion { get; set; } = 14;
}