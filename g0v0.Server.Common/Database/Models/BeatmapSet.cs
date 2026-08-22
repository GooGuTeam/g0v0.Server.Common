// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a beatmap set record used by score submission and spectator replay upload flows.
/// </summary>
[Table("beatmapsets")]
[Index(nameof(Status))]
[Index(nameof(Artist))]
[Index(nameof(ArtistUnicode))]
[Index(nameof(Title))]
[Index(nameof(TitleUnicode))]
[Index(nameof(FeatureArtistTrackId))]
[Index(nameof(Creator))]
[Index(nameof(CreatorId))]
[Index(nameof(HasVideo))]
[Index(nameof(Genre))]
[Index(nameof(Language))]
[Index(nameof(HasStoryboard))]
[Index(nameof(SubmittedDate))]
[Index(nameof(RankedDate))]
[Index(nameof(LastUpdatedDate))]
public class BeatmapSet
{
    /// <summary>
    /// Gets or sets the beatmap set ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the online status of the beatmap set.
    /// </summary>
    public BeatmapOnlineStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the cover image URLs of the beatmap set.
    /// </summary>
    public BeatmapSetOnlineCovers Covers { get; set; }

    /// <summary>
    /// Gets or sets the romanised artist name.
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original artist name.
    /// </summary>
    public string ArtistUnicode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the romanised title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original title.
    /// </summary>
    public string TitleUnicode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the beatmap set contains explicit content.
    /// </summary>
    public bool HasExplicitContent { get; set; }

    /// <summary>
    /// Gets or sets the audio preview URL.
    /// </summary>
    public string PreviewUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of the beatmap set.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the beatmap set is featured in the spotlight.
    /// </summary>
    public bool Spotlight { get; set; }

    /// <summary>
    /// Gets or sets the featured artist track ID.
    /// </summary>
    public int? FeatureArtistTrackId { get; set; }

    /// <summary>
    /// Gets or sets the creator username.
    /// </summary>
    public string Creator { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creator user ID.
    /// </summary>
    public int CreatorId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beatmap set contains a video.
    /// </summary>
    public bool HasVideo { get; set; }

    /// <summary>
    /// Gets or sets the genre of the beatmap set.
    /// </summary>
    public BeatmapSetOnlineGenre Genre { get; set; }

    /// <summary>
    /// Gets or sets the language of the beatmap set.
    /// </summary>
    public BeatmapSetOnlineLanguage Language { get; set; }

    /// <summary>
    /// Gets or sets the current nominations of the beatmap set.
    /// </summary>
    public BeatmapSetOnlineNomination[]? CurrentNominations { get; set; }

    /// <summary>
    /// Gets or sets the description of the beatmap set.
    /// </summary>
    public BeatmapDescription? Description { get; set; }

    /// <summary>
    /// Gets or sets the beats per minute.
    /// </summary>
    public double Bpm { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beatmap set has a storyboard.
    /// </summary>
    public bool HasStoryboard { get; set; }

    /// <summary>
    /// Gets or sets the submission timestamp.
    /// </summary>
    public DateTimeOffset SubmittedDate { get; set; }

    /// <summary>
    /// Gets or sets the ranked timestamp.
    /// </summary>
    public DateTimeOffset? RankedDate { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset? LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the beatmap set tags.
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the difficulties contained in this beatmap set.
    /// </summary>
    [InverseProperty(nameof(Beatmap.BeatmapSet))]
    public ICollection<Beatmap> Beatmaps { get; set; } = new List<Beatmap>();
}