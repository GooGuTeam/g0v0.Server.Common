// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.GeoIP;

/// <summary>
/// Contains available Country and ASN information for an IP address.
/// </summary>
/// <param name="Ip">
/// The normalized IP address.
/// </param>
public sealed record GeoIPLookupResult(string Ip)
{
    /// <summary>
    /// Gets the country ISO code, or null when unavailable.
    /// </summary>
    public string? CountryIso { get; init; }

    /// <summary>
    /// Gets the country name in English, or null when unavailable.
    /// </summary>
    public string? CountryName { get; init; }

    /// <summary>
    /// Gets the autonomous system number, or null when unavailable.
    /// </summary>
    public long? Asn { get; init; }

    /// <summary>
    /// Gets the autonomous system organization, or null when unavailable.
    /// </summary>
    public string? Organization { get; init; }
}