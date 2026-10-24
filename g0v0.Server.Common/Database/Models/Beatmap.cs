// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using osu.Game.Beatmaps;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a beatmap record used by score submission and spectator replay upload flows.
/// </summary>
[Table("beatmaps")]
public class Beatmap
{
    // The legacy column is nullable, so the accessors cannot be an auto property: the field starts
    // as an empty object and a missing value is replaced instead of being exposed.
#pragma warning disable IDE0032
    private BeatmapCovers _covers = new();
#pragma warning restore IDE0032

    /// <summary>
    /// Gets or sets the beatmap ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the beatmap set ID.
    /// </summary>
    public int BeatmapSetId { get; set; }

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

    /// <summary>
    /// Gets or sets the cover art URLs inherited from the beatmap set, persisted as JSON.
    /// </summary>
    /// <remarks>
    /// The osu! API publishes covers on the beatmap set only; the fetcher copies them onto each
    /// difficulty so a beatmap row is enough to render its thumbnails. Rows written before the
    /// column existed hold no covers, so the accessor materialises an empty object instead of
    /// exposing <see langword="null"/>.
    /// </remarks>
    [AllowNull]
    public BeatmapCovers Covers
    {
        get => _covers;
        set => _covers = value ?? new BeatmapCovers();
    }

    /// <summary>
    /// Gets or sets the beatmap set this beatmap belongs to.
    /// </summary>
    [ForeignKey(nameof(BeatmapSetId))]
    public BeatmapSet? BeatmapSet { get; set; }
}