// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.GeoIP;

namespace g0v0.Server.Common.Beatmaps;

/// <summary>Selects a beatmap archive mirror based on the request's country.</summary>
/// <param name="configs">The shared configuration manager.</param>
/// <param name="geoIp">The IP geolocation service.</param>
public sealed class BeatmapDownloadService(ConfigManager configs, IGeoIPService geoIp)
{
    /// <summary>Builds the archive URL, including the correct video selection for each mirror.</summary>
    /// <param name="beatmapSetId">The positive set ID.</param>
    /// <param name="noVideo">Whether video should be omitted.</param>
    /// <param name="address">The client's IP address.</param>
    /// <param name="countryCode">The user's country when geolocation is unavailable.</param>
    /// <returns>The selected mirror's URL.</returns>
    public string GetDownloadUrl(int beatmapSetId, bool noVideo, IPAddress? address, string? countryCode = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(beatmapSetId);
        string? country = address is null ? null : geoIp.Lookup(address).CountryIso;
        bool isChina = string.Equals(string.IsNullOrEmpty(country) ? countryCode : country, "CN", StringComparison.OrdinalIgnoreCase);
        BeatmapDownloadConfig config = configs.TryGet<BeatmapDownloadConfig>() ?? new BeatmapDownloadConfig();
        string template = isChina
            ? (noVideo ? config.ChinaNoVideoUrl : config.ChinaFullUrl)
            : (noVideo ? config.NoVideoUrl : config.FullUrl);
        return template.Replace("{id}", beatmapSetId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}