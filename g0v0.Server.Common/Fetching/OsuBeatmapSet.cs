// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Newtonsoft.Json;
using osu.Game.Beatmaps;

namespace g0v0.Server.Common.Fetching;

/// <summary>
/// Represents a beatmap set payload returned by the osu! API.
/// </summary>
internal sealed class OsuBeatmapSet
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("ranked")]
    public int? Ranked { get; set; }

    [JsonProperty("artist")]
    public string? Artist { get; set; }

    [JsonProperty("artist_unicode")]
    public string? ArtistUnicode { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("title_unicode")]
    public string? TitleUnicode { get; set; }

    [JsonProperty("creator")]
    public string? Creator { get; set; }

    [JsonProperty("user_id")]
    public int CreatorId { get; set; }

    [JsonProperty("preview_url")]
    public string? PreviewUrl { get; set; }

    [JsonProperty("source")]
    public string? Source { get; set; }

    [JsonProperty("tags")]
    public string? Tags { get; set; }

    [JsonProperty("bpm")]
    public double Bpm { get; set; }

    [JsonProperty("nsfw")]
    public bool HasExplicitContent { get; set; }

    [JsonProperty("spotlight")]
    public bool Spotlight { get; set; }

    [JsonProperty("video")]
    public bool HasVideo { get; set; }

    [JsonProperty("storyboard")]
    public bool HasStoryboard { get; set; }

    [JsonProperty("track_id")]
    public int? TrackId { get; set; }

    [JsonProperty("submitted_date")]
    public DateTimeOffset? SubmittedDate { get; set; }

    [JsonProperty("ranked_date")]
    public DateTimeOffset? RankedDate { get; set; }

    [JsonProperty("last_updated")]
    public DateTimeOffset? LastUpdated { get; set; }

    [JsonProperty("covers")]
    public BeatmapSetOnlineCovers? Covers { get; set; }

    [JsonProperty("genre")]
    public BeatmapSetOnlineGenre? Genre { get; set; }

    [JsonProperty("language")]
    public BeatmapSetOnlineLanguage? Language { get; set; }

    [JsonProperty("current_nominations")]
    public BeatmapSetOnlineNomination[]? CurrentNominations { get; set; }

    [JsonProperty("description")]
    public BeatmapDescription? Description { get; set; }

    [JsonProperty("beatmaps")]
    public OsuBeatmap[]? Beatmaps { get; set; }

    [JsonProperty("converts")]
    public OsuBeatmap[]? Converts { get; set; }
}