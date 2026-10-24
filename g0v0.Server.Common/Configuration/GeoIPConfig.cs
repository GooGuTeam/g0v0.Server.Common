// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// Configures the shared Country and ASN database service.
/// </summary>
[ConfigFile("geoip")]
public class GeoIPConfig
{
    /// <summary>
    /// Gets or sets the database directory, relative to the server base path when not absolute.
    /// </summary>
    public string DatabaseDirectory { get; set; } = "geoip";

    /// <summary>
    /// Gets or sets the source directory containing MMDB files and .sha256sum files.
    /// </summary>
    public Uri DatabaseBaseUrl { get; set; } = new("https://raw.githubusercontent.com/Loyalsoldier/geoip/release/");

    /// <summary>
    /// Gets or sets the maximum database age in days before downloading a replacement.
    /// </summary>
    public int MaxAgeDays { get; set; } = 8;

    /// <summary>
    /// Gets or sets the timeout in seconds for each HTTP request.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the interval between background update checks.
    /// </summary>
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromDays(7);
}