// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace g0v0.Server.Common.Fetching;

/// <summary>
/// Represents a beatmap payload returned by the osu! API.
/// </summary>
internal sealed class OsuBeatmap
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("beatmapset_id")]
    public int BeatmapSetId { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("ranked")]
    public int? Ranked { get; set; }

    [JsonProperty("mode_int")]
    public int ModeInt { get; set; }

    [JsonProperty("checksum")]
    public string? Checksum { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("user_id")]
    public int UserId { get; set; }

    [JsonProperty("difficulty_rating")]
    public double DifficultyRating { get; set; }

    [JsonProperty("total_length")]
    public int TotalLength { get; set; }

    [JsonProperty("hit_length")]
    public int HitLength { get; set; }

    [JsonProperty("ar")]
    public float ApproachRate { get; set; }

    [JsonProperty("cs")]
    public float CircleSize { get; set; }

    [JsonProperty("drain")]
    public float DrainRate { get; set; }

    [JsonProperty("accuracy")]
    public float OverallDifficulty { get; set; }

    [JsonProperty("bpm")]
    public float Bpm { get; set; }

    [JsonProperty("count_circles")]
    public int CircleCount { get; set; }

    [JsonProperty("count_sliders")]
    public int SliderCount { get; set; }

    [JsonProperty("count_spinners")]
    public int SpinnerCount { get; set; }

    [JsonProperty("max_combo")]
    public int? MaxCombo { get; set; }

    [JsonProperty("last_updated")]
    public DateTimeOffset? LastUpdated { get; set; }

    [JsonProperty("deleted_at")]
    public DateTimeOffset? DeletedAt { get; set; }

    [JsonProperty("convert")]
    public bool Convert { get; set; }
}