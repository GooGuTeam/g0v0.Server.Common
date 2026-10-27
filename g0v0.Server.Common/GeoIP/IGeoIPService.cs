// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

// ReSharper disable InconsistentNaming
using System.Net;

namespace g0v0.Server.Common.GeoIP;

/// <summary>
/// Provides local GeoIP lookups and verified database updates.
/// </summary>
public interface IGeoIPService
{
    /// <summary>
    /// Looks up an IPv4 or IPv6 address using currently available databases.
    /// </summary>
    /// <param name="ip">The IP address text.</param>
    /// <returns>The available country and ASN information.</returns>
    GeoIPLookupResult Lookup(string ip);

    /// <summary>
    /// Looks up an IP address, normalizing IPv4-mapped IPv6 addresses.
    /// </summary>
    /// <param name="ip">The IP address.</param>
    /// <returns>The available country and ASN information.</returns>
    GeoIPLookupResult Lookup(IPAddress ip);

    /// <summary>
    /// Downloads missing or stale databases and publishes verified replacements.
    /// </summary>
    /// <param name="force">Whether fresh databases should also be replaced.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the update operation.</returns>
    Task UpdateAsync(bool force = false, CancellationToken cancellationToken = default);
}