// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Fetching;
using Newtonsoft.Json;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Stores the cover art URLs of a beatmap, mirroring the <c>covers</c> object of the osu! API.
/// </summary>
/// <remarks>
/// The osu! API only publishes covers on the beatmap set, so a beatmap row inherits the covers of
/// its set when it is fetched. The object is persisted as JSON so the exact URLs - including the
/// cache-busting query string - survive a round trip.
/// </remarks>
public class BeatmapCovers
{
    /// <summary>
    /// Gets or sets the standard cover URL.
    /// </summary>
    [JsonProperty("cover")]
    public string Cover { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the double-resolution cover URL.
    /// </summary>
    [JsonProperty("cover@2x")]
    public string Cover2X { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cover card URL.
    /// </summary>
    [JsonProperty("card")]
    public string Card { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the double-resolution cover card URL.
    /// </summary>
    [JsonProperty("card@2x")]
    public string Card2X { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list thumbnail URL.
    /// </summary>
    [JsonProperty("list")]
    public string List { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the double-resolution list thumbnail URL.
    /// </summary>
    [JsonProperty("list@2x")]
    public string List2X { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the slim cover URL.
    /// </summary>
    [JsonProperty("slimcover")]
    public string SlimCover { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the double-resolution slim cover URL.
    /// </summary>
    [JsonProperty("slimcover@2x")]
    public string SlimCover2X { get; set; } = string.Empty;

    /// <summary>
    /// Creates covers from the osu! API beatmap set covers.
    /// </summary>
    /// <param name="covers">
    /// The API covers, or <see langword="null"/> when the set has none.
    /// </param>
    /// <returns>
    /// The mapped covers; an empty object when <paramref name="covers"/> is null.
    /// </returns>
    internal static BeatmapCovers FromBeatmapSet(OsuBeatmapSetCovers? covers) => covers is null
        ? new BeatmapCovers()
        : new BeatmapCovers
        {
            Cover = covers.Cover ?? string.Empty,
            Cover2X = covers.Cover2X ?? string.Empty,
            Card = covers.Card ?? string.Empty,
            Card2X = covers.Card2X ?? string.Empty,
            List = covers.List ?? string.Empty,
            List2X = covers.List2X ?? string.Empty,
            SlimCover = covers.SlimCover ?? string.Empty,
            SlimCover2X = covers.SlimCover2X ?? string.Empty,
        };

    /// <summary>
    /// Creates the game library's covers value from the osu! API covers.
    /// </summary>
    /// <param name="covers">
    /// The API covers, or <see langword="null"/> when the set has none.
    /// </param>
    /// <returns>
    /// The mapped covers.
    /// </returns>
    internal static osu.Game.Beatmaps.BeatmapSetOnlineCovers ToGameCovers(OsuBeatmapSetCovers? covers)
    {
        return covers is not null
            ? new osu.Game.Beatmaps.BeatmapSetOnlineCovers
            {
                CoverLowRes = covers.Cover ?? string.Empty,
                Cover = covers.Cover2X ?? string.Empty,
                CardLowRes = covers.Card ?? string.Empty,
                Card = covers.Card2X ?? string.Empty,
                ListLowRes = covers.List ?? string.Empty,
                List = covers.List2X ?? string.Empty,
            }
            : default;
    }
}