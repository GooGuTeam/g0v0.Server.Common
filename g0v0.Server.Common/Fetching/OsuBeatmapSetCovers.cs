// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace g0v0.Server.Common.Fetching;

/// <summary>
/// Represents the <c>covers</c> object the osu! API publishes for a beatmap set.
/// </summary>
/// <remarks>
/// The names carry explicit JSON properties because the API uses <c>cover</c> for the standard
/// image and <c>cover@2x</c> for the double-resolution one; relying on property names would drop
/// the lower-resolution URLs.
/// </remarks>
internal sealed class OsuBeatmapSetCovers
{
    [JsonProperty("cover")]
    public string? Cover { get; set; }

    [JsonProperty("cover@2x")]
    public string? Cover2X { get; set; }

    [JsonProperty("card")]
    public string? Card { get; set; }

    [JsonProperty("card@2x")]
    public string? Card2X { get; set; }

    [JsonProperty("list")]
    public string? List { get; set; }

    [JsonProperty("list@2x")]
    public string? List2X { get; set; }

    [JsonProperty("slimcover")]
    public string? SlimCover { get; set; }

    [JsonProperty("slimcover@2x")]
    public string? SlimCover2X { get; set; }

    /// <summary>
    /// Creates the game library's covers value from this API covers object.
    /// </summary>
    /// <returns>The mapped covers.</returns>
    internal osu.Game.Beatmaps.BeatmapSetOnlineCovers ToGameCovers() => new()
    {
        CoverLowRes = Cover ?? string.Empty,
        Cover = Cover2X ?? string.Empty,
        CardLowRes = Card ?? string.Empty,
        Card = Card2X ?? string.Empty,
        ListLowRes = List ?? string.Empty,
        List = List2X ?? string.Empty,
    };
}